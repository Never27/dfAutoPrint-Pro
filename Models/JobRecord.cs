namespace PdfAutoPrint.Pro.Models;

/// <summary>
/// 打印任务记录
/// </summary>
public class JobRecord
{
    public long Id { get; set; }
    public string PrinterName { get; set; } = "";
    public string PrinterId { get; set; } = "";

    /// <summary>作业名</summary>
    public string? JobName { get; set; }

    /// <summary>打印者</summary>
    public string? Author { get; set; }

    /// <summary>源文件名</summary>
    public string SourceFile { get; set; } = "";

    /// <summary>输出的PDF路径</summary>
    public string OutputFile { get; set; } = "";

    /// <summary>文件大小(字节)</summary>
    public long FileSize { get; set; }

    /// <summary>页数</summary>
    public int PageCount { get; set; }

    /// <summary>状态: Success/Failed/Skipped</summary>
    public string Status { get; set; } = "Success";

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>开始处理时间</summary>
    public DateTime StartTime { get; set; } = DateTime.Now;

    /// <summary>完成时间</summary>
    public DateTime? EndTime { get; set; }

    /// <summary>耗时(毫秒)</summary>
    public int DurationMs { get; set; }
}
