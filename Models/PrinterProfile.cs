namespace PdfAutoPrint.Pro.Models;

/// <summary>
/// 单个虚拟打印机的完整配置
/// </summary>
public class PrinterProfile
{
    /// <summary>唯一标识</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>打印机名称（Windows中显示的名称）</summary>
    public string PrinterName { get; set; } = "Auto PDF Printer";

    /// <summary>是否启用监控</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>端口名称（spool文件路径）</summary>
    public string PortName { get; set; } = @"C:\PDFOutput\spool\printjob.prn";

    /// <summary>打印机驱动名称</summary>
    public string DriverName { get; set; } = "Microsoft PS Class Driver";

    // ---- 输出配置 ----
    public OutputConfig Output { get; set; } = new();

    // ---- 文件命名模板 ----
    public FileNameTemplate FileName { get; set; } = new();

    // ---- 冲突处理 ----
    public ConflictConfig Conflict { get; set; } = new();

    // ---- 通知配置 ----
    public NotificationConfig Notification { get; set; } = new();

    // ---- 输出质量 ----
    public QualityConfig Quality { get; set; } = new();

    // ---- 水印配置 ----
    public WatermarkConfig Watermark { get; set; } = new();

    // ---- 服务配置 ----
    public ServiceConfig Service { get; set; } = new();
}

/// <summary>
/// 输出配置：自动保存/弹框、输出目录、日期文件夹
/// </summary>
public class OutputConfig
{
    /// <summary>true=自动保存，false=弹框选择</summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>输出根目录，支持占位符如 {yyyy-MM-dd}</summary>
    public string OutputRoot { get; set; } = @"C:\PDFOutput\{yyyy-MM-dd}";

    /// <summary>监控的spool目录</summary>
    public string WatchFolder { get; set; } = @"C:\PDFOutput\spool";

    /// <summary>转换后删除源文件</summary>
    public bool DeleteSourceAfterConvert { get; set; } = true;
}

/// <summary>
/// 文件名模板：支持打印作业元数据占位符
/// </summary>
public class FileNameTemplate
{
    /// <summary>
    /// 文件名模式，支持占位符：
    /// &lt;PrintJobName&gt; 打印作业名
    /// &lt;PrintJobAuthor&gt; 打印者
    /// &lt;PrintJobTime:format&gt; 打印时间
    /// &lt;DateTime:format&gt; 当前时间
    /// &lt;Counter&gt; 自增计数
    /// 固定文本直接写
    /// 默认: &lt;PrintJobName&gt;_&lt;DateTime:yyyyMMdd_HHmmss&gt;
    /// </summary>
    public string Pattern { get; set; } = "<PrintJobName>_<DateTime:yyyyMMdd_HHmmss>";

    /// <summary>当无法获取作业名时的默认前缀</summary>
    public string DefaultPrefix { get; set; } = "Print";
}

/// <summary>
/// 文件冲突处理策略
/// </summary>
public enum ConflictMode
{
    /// <summary>直接覆盖</summary>
    Overwrite,
    /// <summary>文件名末尾追加连接符+递增数字 (file_1.pdf, file_2.pdf)</summary>
    Increment,
    /// <summary>原文件重命名为 文件名+日期时间[备份]</summary>
    Backup
}

public class ConflictConfig
{
    public ConflictMode Mode { get; set; } = ConflictMode.Increment;

    /// <summary>递增连接符，默认 "_"</summary>
    public string IncrementSeparator { get; set; } = "_";

    /// <summary>备份后缀日期格式</summary>
    public string BackupSuffixFormat { get; set; } = "yyyyMMddHHmmss[备份]";
}

/// <summary>
/// 系统托盘通知配置
/// </summary>
public class NotificationConfig
{
    /// <summary>是否启用系统托盘</summary>
    public bool EnableSystemTray { get; set; } = true;

    /// <summary>成功时弹气泡通知</summary>
    public bool NotifyOnSuccess { get; set; } = true;

    /// <summary>失败时弹气泡通知</summary>
    public bool NotifyOnFailure { get; set; } = true;
}

/// <summary>
/// 输出质量配置
/// </summary>
public enum ColorMode
{
    /// <summary>彩色输出</summary>
    Color,
    /// <summary>黑白/灰度输出</summary>
    Grayscale
}

public enum OutputMode
{
    /// <summary>保持原文格式（矢量文字可选中）</summary>
    Original,
    /// <summary>图片模式（内容不变但无法编辑文字）</summary>
    Image
}

public class QualityConfig
{
    public ColorMode Color { get; set; } = ColorMode.Color;
    public OutputMode Mode { get; set; } = OutputMode.Original;
    /// <summary>图片模式下的DPI（仅Image模式有效）</summary>
    public int ImageDpi { get; set; } = 300;
}

/// <summary>
/// 日志配置
/// </summary>
public class LogConfig
{
    /// <summary>日志保留天数，0=永久保留</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>日志级别</summary>
    public string LogLevel { get; set; } = "Info";
}

/// <summary>
/// 服务与容错配置
/// </summary>
public class ServiceConfig
{
    /// <summary>操作失败时是否跳过（不中断处理流程）</summary>
    public bool SkipOnFailure { get; set; } = true;

    /// <summary>GhostScript路径，null=自动查找</summary>
    public string? GhostScriptPath { get; set; }

    /// <summary>开机自启</summary>
    public bool AutoStart { get; set; } = false;
}

/// <summary>
/// 全局配置
/// </summary>
public class GlobalConfig
{
    /// <summary>所有打印机配置</summary>
    public List<PrinterProfile> Printers { get; set; } = new()
    {
        new PrinterProfile()
    };

    /// <summary>日志配置</summary>
    public LogConfig Log { get; set; } = new();

    /// <summary>开机自启（全局）</summary>
    public bool AutoStart { get; set; } = false;
}
