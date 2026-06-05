using System.IO;
using System.Text.Json;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 配置管理服务：加载/保存全局配置和打印机配置
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PdfAutoPrint.Pro");

    private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public GlobalConfig Config { get; private set; } = new();

    public ConfigService()
    {
        Directory.CreateDirectory(ConfigDir);
    }

    public void Load()
    {
        try
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                Config = JsonSerializer.Deserialize<GlobalConfig>(json, JsonOptions) ?? new GlobalConfig();
            }
            else
            {
                Config = CreateDefault();
                Save();
            }
        }
        catch
        {
            Config = CreateDefault();
        }
    }

    public void Save()
    {
        var json = JsonSerializer.Serialize(Config, JsonOptions);
        File.WriteAllText(ConfigFile, json);
    }

    public PrinterProfile AddPrinter(string name)
    {
        var profile = new PrinterProfile
        {
            Id = Guid.NewGuid().ToString("N")[..8],
            PrinterName = name,
            PortName = $@"C:\PDFOutput\spool\{name.Replace(" ", "_")}.prn",
            Output = new OutputConfig
            {
                WatchFolder = $@"C:\PDFOutput\spool\{name.Replace(" ", "_")}"
            }
        };
        Config.Printers.Add(profile);
        Save();
        return profile;
    }

    public void RemovePrinter(string id)
    {
        Config.Printers.RemoveAll(p => p.Id == id);
        Save();
    }

    public PrinterProfile? GetPrinter(string id)
    {
        return Config.Printers.FirstOrDefault(p => p.Id == id);
    }

    private static GlobalConfig CreateDefault()
    {
        return new GlobalConfig
        {
            Printers = new List<PrinterProfile>
            {
                new PrinterProfile
                {
                    Id = "default",
                    PrinterName = "Auto PDF Printer",
                    PortName = @"C:\PDFOutput\spool\printjob.prn",
                    Output = new OutputConfig
                    {
                        WatchFolder = @"C:\PDFOutput\spool"
                    }
                }
            },
            Log = new LogConfig { RetentionDays = 30 }
        };
    }
}
