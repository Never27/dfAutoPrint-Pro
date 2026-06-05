using System.IO;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 日志服务：控制台+文件输出，支持保留天数清理
/// </summary>
public class LogService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PdfAutoPrint.Pro", "logs");

    private readonly LogConfig _config;
    private readonly object _lock = new();
    private string _currentLogFile = "";

    public event Action<LogEntry>? OnLogEntry;

    public LogService(LogConfig config)
    {
        _config = config;
        Directory.CreateDirectory(LogDir);
        CleanOldLogs();
    }

    public void Info(string message, string? source = null)
    {
        Log("INFO", message, source);
    }

    public void Warn(string message, string? source = null)
    {
        Log("WARN", message, source);
    }

    public void Error(string message, Exception? ex = null, string? source = null)
    {
        Log("ERROR", ex != null ? $"{message} | {ex.Message}" : message, source);
    }

    public void Success(string message, string? source = null)
    {
        Log("SUCCESS", message, source);
    }

    public void Debug(string message, string? source = null)
    {
        if (_config.LogLevel == "Debug")
            Log("DEBUG", message, source);
    }

    private void Log(string level, string message, string? source)
    {
        var entry = new LogEntry
        {
            Time = DateTime.Now,
            Level = level,
            Message = message,
            Source = source ?? "System"
        };

        lock (_lock)
        {
            EnsureLogFile();
            try
            {
                File.AppendAllText(_currentLogFile,
                    $"[{entry.Time:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level,-7}] [{entry.Source}] {entry.Message}{Environment.NewLine}");
            }
            catch { /* 日志写入失败不影响主流程 */ }
        }

        OnLogEntry?.Invoke(entry);
    }

    private void EnsureLogFile()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var expectedFile = Path.Combine(LogDir, $"{today}.log");
        if (_currentLogFile != expectedFile)
        {
            _currentLogFile = expectedFile;
        }
    }

    /// <summary>
    /// 清理过期日志
    /// </summary>
    public void CleanOldLogs()
    {
        if (_config.RetentionDays <= 0) return; // 永久保留

        try
        {
            var cutoff = DateTime.Now.AddDays(-_config.RetentionDays);
            var files = Directory.GetFiles(LogDir, "*.log");
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParseExact(name, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out var date))
                {
                    if (date < cutoff)
                    {
                        File.Delete(file);
                    }
                }
            }
        }
        catch { /* 清理失败不影响 */ }
    }

    /// <summary>
    /// 获取日志目录路径
    /// </summary>
    public string GetLogDirectory() => LogDir;

    /// <summary>
    /// 获取所有日志文件列表（按日期倒序）
    /// </summary>
    public List<string> GetLogFiles()
    {
        try
        {
            return Directory.GetFiles(LogDir, "*.log")
                .OrderByDescending(f => f)
                .ToList();
        }
        catch { return new List<string>(); }
    }

    /// <summary>
    /// 读取指定日志文件内容
    /// </summary>
    public string ReadLogFile(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch { return ""; }
    }
}

public class LogEntry
{
    public DateTime Time { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
}
