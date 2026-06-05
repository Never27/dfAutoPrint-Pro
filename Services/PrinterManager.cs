using System.Diagnostics;
using System.IO;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 打印机管理服务：创建/删除虚拟打印机、系统打印机管理
/// 通过PowerShell WMI查询实现，无额外依赖
/// </summary>
public class PrinterManager
{
    public bool IsAdministrator => new System.Security.Principal.WindowsPrincipal(
        System.Security.Principal.WindowsIdentity.GetCurrent())
        .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

    /// <summary>
    /// 获取系统所有打印机列表
    /// </summary>
    public async Task<List<PrinterInfo>> GetSystemPrintersAsync()
    {
        var printers = new List<PrinterInfo>();
        var script = @"
            Get-WmiObject -Class Win32_Printer | Select-Object Name, DriverName, PortName, Default, PrinterStatus, Shared |
            ForEach-Object {
                Write-Output ""$($_.Name)||$($_.DriverName)||$($_.PortName)||$($_.Default)||$($_.PrinterStatus)||$($_.Shared)""
            }
        ";

        var output = await RunPowerShellAsync(script);
        if (output == null) return printers;

        foreach (var line in output.Split('\n'))
        {
            var parts = line.Trim().Split("||");
            if (parts.Length >= 6)
            {
                printers.Add(new PrinterInfo
                {
                    Name = parts[0],
                    DriverName = parts[1],
                    PortName = parts[2],
                    IsDefault = parts[3] == "True",
                    Status = parts[4] == "3" ? "空闲" : "未知",
                    IsShared = parts[5] == "True"
                });
            }
        }
        return printers;
    }

    /// <summary>
    /// 获取可用PostScript驱动列表
    /// </summary>
    public async Task<List<string>> GetPsDriversAsync()
    {
        var drivers = new List<string>();
        var script = @"
            Get-WmiObject -Class Win32_PrinterDriver | Select-Object Name |
            ForEach-Object { Write-Output $_.Name }
        ";

        var output = await RunPowerShellAsync(script);
        if (output == null) return drivers;

        foreach (var line in output.Split('\n'))
        {
            var name = line.Trim();
            if (!string.IsNullOrEmpty(name) &&
                (name.Contains("PS", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("PostScript", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Imagesetter", StringComparison.OrdinalIgnoreCase) ||
                 name.Contains("Publisher", StringComparison.OrdinalIgnoreCase)))
            {
                drivers.Add(name);
            }
        }
        return drivers;
    }

    /// <summary>
    /// 创建虚拟PDF打印机
    /// </summary>
    public async Task<bool> CreateVirtualPrinterAsync(PrinterProfile profile)
    {
        if (!IsAdministrator)
            throw new UnauthorizedAccessException("创建打印机需要管理员权限");

        Directory.CreateDirectory(Path.GetDirectoryName(profile.PortName) ?? @"C:\PDFOutput\spool");

        var script = $@"
            $ErrorActionPreference = 'Stop'
            try {{
                $port = '{EscapePs(profile.PortName)}'
                $name = '{EscapePs(profile.PrinterName)}'
                $driver = '{EscapePs(profile.DriverName)}'
                
                Remove-PrinterPort -Name $port -ErrorAction SilentlyContinue
                Add-PrinterPort -Name $port -ErrorAction Stop
                
                Remove-Printer -Name $name -ErrorAction SilentlyContinue
                Add-Printer -Name $name -DriverName $driver -PortName $port -ErrorAction Stop
                
                Write-Output 'OK'
            }} catch {{
                Write-Output ""ERROR:$($_.Exception.Message)""
            }}
        ";

        var output = await RunPowerShellAsync(script);
        return output?.Trim() == "OK";
    }

    public async Task<bool> DeletePrinterAsync(string printerName)
    {
        var script = $@"
            Remove-Printer -Name '{EscapePs(printerName)}' -ErrorAction SilentlyContinue
            Write-Output 'OK'
        ";
        return (await RunPowerShellAsync(script))?.Trim() == "OK";
    }

    public async Task<bool> SetDefaultPrinterAsync(string printerName)
    {
        var script = $@"
            (New-Object -ComObject WScript.Network).SetDefaultPrinter('{EscapePs(printerName)}')
            Write-Output 'OK'
        ";
        return (await RunPowerShellAsync(script))?.Trim() == "OK";
    }

    public async Task<bool> RestartSpoolerAsync()
    {
        var script = @"
            Restart-Service -Name Spooler -Force
            Start-Sleep -Seconds 2
            Write-Output 'OK'
        ";
        return (await RunPowerShellAsync(script))?.Trim() == "OK";
    }

    public async Task<List<PrintJobInfo>> GetPrintQueueAsync(string printerName)
    {
        var jobs = new List<PrintJobInfo>();
        var script = $@"
            Get-WmiObject -Class Win32_PrintJob | Where-Object {{ $_.Name -like '*{EscapePs(printerName)}*' }} |
            ForEach-Object {{
                Write-Output ""$($_.JobId)||$($_.Document)||$($_.Owner)||$($_.Status)||$($_.TotalPages)||$($_.TimeSubmitted)""
            }}
        ";

        var output = await RunPowerShellAsync(script);
        if (output == null) return jobs;

        foreach (var line in output.Split('\n'))
        {
            var parts = line.Trim().Split("||");
            if (parts.Length >= 6 && uint.TryParse(parts[0], out var jobId))
            {
                jobs.Add(new PrintJobInfo
                {
                    JobId = jobId,
                    Document = parts[1],
                    Owner = parts[2],
                    Status = parts[3],
                    TotalPages = uint.TryParse(parts[4], out var pages) ? pages : 0,
                    Submitted = DateTime.Now
                });
            }
        }
        return jobs;
    }

    public bool PrinterExists(string printerName)
    {
        var output = RunPowerShellAsync($@"
            $p = Get-Printer -Name '{EscapePs(printerName)}' -ErrorAction SilentlyContinue
            if ($p) {{ Write-Output 'EXISTS' }} else {{ Write-Output 'NOTFOUND' }}
        ").Result;
        return output?.Trim() == "EXISTS";
    }

    private static async Task<string?> RunPowerShellAsync(string script)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(psi);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit(5000);
            return output;
        }
        catch { return null; }
    }

    private static string EscapePs(string s) => s.Replace("'", "''");
}

public class PrinterInfo
{
    public string Name { get; set; } = "";
    public string DriverName { get; set; } = "";
    public string PortName { get; set; } = "";
    public bool IsDefault { get; set; }
    public string Status { get; set; } = "";
    public bool IsShared { get; set; }
}

public class PrintJobInfo
{
    public uint JobId { get; set; }
    public string Document { get; set; } = "";
    public string Owner { get; set; } = "";
    public string Status { get; set; } = "";
    public uint TotalPages { get; set; }
    public DateTime Submitted { get; set; }
}
