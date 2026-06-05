using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 打印任务监控调度器：监听spool目录，协调GS转换、水印注入、文件命名
/// </summary>
public class SpoolMonitor
{
    private readonly PrinterProfile _profile;
    private readonly GhostScriptService _gs;
    private readonly PsWatermarkEngine _watermark;
    private readonly FileNameResolver _nameResolver;
    private readonly ConflictResolver _conflictResolver;
    private readonly LogService _log;
    private readonly JobHistoryService _history;
    private readonly PrinterManager _printerManager;

    private FileSystemWatcher? _watcher;
    private CancellationTokenSource? _cts;
    private readonly ConcurrentDictionary<string, bool> _processing = new();

    public event Action<JobRecord>? OnJobCompleted;
    public event Action<JobRecord>? OnJobFailed;

    public SpoolMonitor(
        PrinterProfile profile,
        GhostScriptService gs,
        PsWatermarkEngine watermark,
        FileNameResolver nameResolver,
        ConflictResolver conflictResolver,
        LogService log,
        JobHistoryService history,
        PrinterManager printerManager)
    {
        _profile = profile;
        _gs = gs;
        _watermark = watermark;
        _nameResolver = nameResolver;
        _conflictResolver = conflictResolver;
        _log = log;
        _history = history;
        _printerManager = printerManager;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        if (!Directory.Exists(_profile.Output.WatchFolder))
            Directory.CreateDirectory(_profile.Output.WatchFolder);

        _watcher = new FileSystemWatcher(_profile.Output.WatchFolder)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        _watcher.Created += OnFileCreated;
        _watcher.Changed += OnFileChanged;

        _log.Info($"监控已启动: {_profile.PrinterName} -> {_profile.Output.WatchFolder}",
            _profile.PrinterName);

        await Task.Delay(-1, _cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _watcher?.Dispose();
        _watcher = null;
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!IsValidFile(e.FullPath)) return;
        await ProcessFileAsync(e.FullPath);
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!IsValidFile(e.FullPath)) return;
        // Changed事件防抖：延迟处理
        await Task.Delay(500);
        await ProcessFileAsync(e.FullPath);
    }

    private bool IsValidFile(string path)
    {
        var ext = Path.GetExtension(path).ToLower();
        return ext is ".prn" or ".ps" && !_processing.ContainsKey(path);
    }

    private async Task ProcessFileAsync(string filePath)
    {
        if (!_processing.TryAdd(filePath, true)) return;

        var record = new JobRecord
        {
            PrinterName = _profile.PrinterName,
            PrinterId = _profile.Id,
            SourceFile = filePath,
            StartTime = DateTime.Now
        };

        try
        {
            _log.Info($"检测到打印任务: {Path.GetFileName(filePath)}", _profile.PrinterName);

            // 等待文件写入完成
            await WaitForFileReadyAsync(filePath);

            // 读取PostScript内容
            string psContent;
            try
            {
                psContent = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                _log.Error($"读取文件失败: {ex.Message}", ex, _profile.PrinterName);
                if (_profile.Service.SkipOnFailure)
                {
                    record.Status = "Skipped";
                    record.ErrorMessage = ex.Message;
                    _ = _history.AddRecordAsync(record);
                    return;
                }
                throw;
            }

            // 提取元数据
            var metadata = ExtractMetadata(psContent);

            // 从打印队列获取真实文档名（WMI），覆盖 PS DSC 中不可靠的 %%Title
            await OverrideJobNameFromPrintQueueAsync(metadata, _profile.PrinterName);

            record.JobName = metadata.GetValueOrDefault("PrintJobName",
                metadata.GetValueOrDefault("Title", Path.GetFileNameWithoutExtension(filePath)));

            // 注入水印
            if (_profile.Watermark.Enabled)
            {
                psContent = _watermark.InjectWatermarks(psContent, _profile.Watermark, record.JobName);
            }

            // 写回临时文件
            var tempPs = Path.GetTempFileName() + ".ps";
            File.WriteAllText(tempPs, psContent);

            // 生成输出路径
            var outputDir = FileNameResolver.ResolveOutputRoot(_profile.Output.OutputRoot);
            Directory.CreateDirectory(outputDir);

            var baseName = _nameResolver.Resolve(_profile.FileName, metadata);
            var outputFile = Path.Combine(outputDir, $"{baseName}.pdf");

            // 冲突处理
            outputFile = _conflictResolver.Resolve(outputFile, _profile.Conflict);

            // GhostScript转换
            var success = await _gs.ConvertToPdfAsync(tempPs, outputFile, _profile.Quality,
                _cts?.Token ?? CancellationToken.None);

            // 清理临时文件
            try { File.Delete(tempPs); } catch { }

            if (success && File.Exists(outputFile))
            {
                var fileInfo = new FileInfo(outputFile);
                record.Status = "Success";
                record.OutputFile = outputFile;
                record.FileSize = fileInfo.Length;
                record.EndTime = DateTime.Now;
                record.DurationMs = (int)(record.EndTime.Value - record.StartTime).TotalMilliseconds;

                _log.Success($"转换完成: {Path.GetFileName(outputFile)} ({FormatSize(fileInfo.Length)})",
                    _profile.PrinterName);

                OnJobCompleted?.Invoke(record);
            }
            else
            {
                throw new Exception("GhostScript转换失败");
            }
        }
        catch (Exception ex)
        {
            record.Status = _profile.Service.SkipOnFailure ? "Skipped" : "Failed";
            record.ErrorMessage = ex.Message;
            record.EndTime = DateTime.Now;
            record.DurationMs = (int)(record.EndTime.Value - record.StartTime).TotalMilliseconds;

            _log.Error($"处理失败: {ex.Message}", ex, _profile.PrinterName);
            OnJobFailed?.Invoke(record);
        }
        finally
        {
            _processing.TryRemove(filePath, out _);

            // 保存记录
            await _history.AddRecordAsync(record);

            // 删除源文件
            if (_profile.Output.DeleteSourceAfterConvert)
            {
                try { File.Delete(filePath); } catch { }
            }
        }
    }

    private static async Task WaitForFileReadyAsync(string filePath, int maxRetries = 20)
    {
        long lastSize = -1;
        int stableCount = 0;

        for (int i = 0; i < maxRetries; i++)
        {
            await Task.Delay(200);
            try
            {
                var info = new FileInfo(filePath);
                if (!info.Exists) return;

                if (info.Length == lastSize)
                {
                    stableCount++;
                    if (stableCount >= 3) return; // 连续3次大小不变
                }
                else
                {
                    lastSize = info.Length;
                    stableCount = 0;
                }
            }
            catch { return; }
        }
    }

    private static Dictionary<string, string> ExtractMetadata(string psContent)
    {
        var meta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in psContent.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("%%")) continue;

            // %%Title: xxx
            if (TryExtractDsc(trimmed, "%%Title:", out var title))
                meta["Title"] = title;
            else if (TryExtractDsc(trimmed, "%%Creator:", out var creator))
                meta["Creator"] = creator;
            else if (TryExtractDsc(trimmed, "%%For:", out var author))
                meta["Author"] = author;
            else if (TryExtractDsc(trimmed, "%%CreationDate:", out var date))
                meta["CreationDate"] = date;
        }

        // 确保基本字段
        if (!meta.ContainsKey("Title"))
            meta["PrintJobName"] = "Untitled";
        else
            meta["PrintJobName"] = meta["Title"];

        if (!meta.ContainsKey("Author"))
            meta["PrintJobAuthor"] = Environment.UserName;
        else
            meta["PrintJobAuthor"] = meta["Author"];

        return meta;
    }

    /// <summary>
    /// 从 Windows 打印队列（WMI）获取真实文档名，覆盖 PS DSC 中不可靠的 %%Title
    /// </summary>
    private async Task OverrideJobNameFromPrintQueueAsync(
        Dictionary<string, string> metadata, string printerName)
    {
        try
        {
            var jobs = await GetPrintQueueWithTimeoutAsync(printerName, 2500);
            // 取最近提交的作业（最高 JobId）
            var lastJob = jobs.OrderByDescending(j => j.JobId).FirstOrDefault();
            if (lastJob != null)
            {
                var docName = CleanDocumentName(lastJob.Document);
                if (!string.IsNullOrWhiteSpace(docName) && !IsGenericPsName(docName))
                {
                    metadata["PrintJobName"] = docName;
                    metadata["Title"] = docName;
                }
            }
        }
        catch (Exception)
        {
            // WMI 查询失败时静默回退到 PS DSC 元数据
        }
    }

    private async Task<List<PrintJobInfo>> GetPrintQueueWithTimeoutAsync(
        string printerName, int timeoutMs)
    {
        var task = _printerManager.GetPrintQueueAsync(printerName);
        var timeout = Task.Delay(timeoutMs);
        var completed = await Task.WhenAny(task, timeout);
        if (completed == timeout) return new List<PrintJobInfo>();
        return await task;
    }

    /// <summary>
    /// 从原始文档名字符串中提取干净的文件名
    /// Win32_PrintJob.Document 可能返回完整路径或带扩展名的文件名
    /// </summary>
    private static string CleanDocumentName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "";

        // 去除完整路径，只保留文件名
        if (rawName.Contains('\\') || rawName.Contains('/'))
            rawName = Path.GetFileNameWithoutExtension(rawName);
        else if (rawName.Contains('.') && !rawName.StartsWith("."))
            rawName = Path.GetFileNameWithoutExtension(rawName);

        return rawName.Trim();
    }

    /// <summary>
    /// 判断是否为 XPS-to-PS 转换器生成的通用占位名
    /// </summary>
    private static bool IsGenericPsName(string name)
    {
        return name.StartsWith("MSxpsPS", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("LocalDownlevel", StringComparison.OrdinalIgnoreCase) ||
               name.StartsWith("RemoteDownlevel", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryExtractDsc(string line, string prefix, out string value)
    {
        value = "";
        if (!line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        value = line[prefix.Length..].Trim();
        return !string.IsNullOrEmpty(value);
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1}KB";
        return $"{bytes / (1024.0 * 1024.0):F1}MB";
    }
}
