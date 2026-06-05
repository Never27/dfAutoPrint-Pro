using System.Diagnostics;
using System.IO;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// GhostScript 转换服务：PS/EPS→PDF，支持彩色/灰度/图片模式
/// </summary>
public class GhostScriptService
{
    private string? _gsPath;

    public string GhostScriptPath => _gsPath ?? throw new InvalidOperationException("GhostScript not found");

    public bool Initialize(string? customPath = null)
    {
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
        {
            _gsPath = customPath;
            return true;
        }

        _gsPath = FindGhostScript();
        return _gsPath != null;
    }

    /// <summary>
    /// 将PostScript文件转换为PDF
    /// </summary>
    public async Task<bool> ConvertToPdfAsync(
        string inputFile,
        string outputFile,
        QualityConfig? quality = null,
        CancellationToken ct = default)
    {
        if (_gsPath == null) throw new InvalidOperationException("GhostScript not initialized");

        var args = BuildArguments(inputFile, outputFile, quality);

        var psi = new ProcessStartInfo
        {
            FileName = _gsPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        try
        {
            var process = Process.Start(psi);
            if (process == null) return false;

            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(ct);
            var error = await errorTask;

            return process.ExitCode == 0 && File.Exists(outputFile);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GS conversion error: {ex.Message}");
            return false;
        }
    }

    private static string BuildArguments(string input, string output, QualityConfig? quality)
    {
        var args = new List<string>
        {
            "-dNOPAUSE",
            "-dQUIET",
            "-dBATCH",
            "-dSAFER",
            "-sDEVICE=pdfwrite",
            // 防止内容裁切：缩放适配页面 / 禁止自动旋转
            "-dPDFFitPage",
            "-dAutoRotatePages=/None"
        };

        quality ??= new QualityConfig();

        // 色彩模式
        if (quality.Color == ColorMode.Grayscale)
        {
            args.Add("-sColorConversionStrategy=Gray");
            args.Add("-dProcessColorModel=/DeviceGray");
        }
        else
        {
            args.Add("-sColorConversionStrategy=RGB");
        }

        // 输出模式
        if (quality.Mode == OutputMode.Image)
        {
            // 图片模式：将所有内容渲染为位图再嵌入PDF
            args.Add($"-r{quality.ImageDpi}");
            args.Add("-dHaveTransparency=false");
            args.Add("-dCompatibilityLevel=1.4");
        }
        else
        {
            // 原始模式：保持矢量文字
            args.Add("-dPDFSETTINGS=/printer");
            args.Add("-dEmbedAllFonts=true");
            args.Add("-dSubsetFonts=true");
            args.Add("-dCompatibilityLevel=1.7");
        }

        args.Add($"-sOutputFile=\"{output}\"");
        args.Add($"\"{input}\"");

        return string.Join(" ", args);
    }

    private static string? FindGhostScript()
    {
        // 优先查找Program Files下的各版本
        var programDirs = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        var versions = new[] { "gs10.04.0", "gs10.03.0", "gs10.02.0", "gs10.01.0", "gs10.00.0", "gs9.55.0", "gs9.54.0", "gs9.53.0", "gs9.52.0", "gs9.50" };

        foreach (var dir in programDirs)
        {
            var gsDir = Path.Combine(dir, "gs");
            if (!Directory.Exists(gsDir)) continue;

            foreach (var ver in versions)
            {
                var exe = Path.Combine(gsDir, ver, "bin", "gswin64c.exe");
                if (File.Exists(exe)) return exe;
                exe = Path.Combine(gsDir, ver, "bin", "gswin32c.exe");
                if (File.Exists(exe)) return exe;
            }
        }

        // 通过PATH查找
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var p in pathEnv.Split(';'))
        {
            var exe = Path.Combine(p.Trim(), "gswin64c.exe");
            if (File.Exists(exe)) return exe;
            exe = Path.Combine(p.Trim(), "gswin32c.exe");
            if (File.Exists(exe)) return exe;
        }

        return null;
    }
}
