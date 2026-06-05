using System.IO;
using System.Text.RegularExpressions;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// 文件名解析器：根据模板生成最终文件名
/// 支持占位符: &lt;PrintJobName&gt; &lt;PrintJobAuthor&gt; &lt;PrintJobTime:format&gt; &lt;DateTime:format&gt; &lt;Counter&gt; {date}
/// </summary>
public class FileNameResolver
{
    private readonly Dictionary<string, int> _counters = new();

    /// <summary>
    /// 解析文件名模板，返回最终文件名（不含扩展名）
    /// </summary>
    public string Resolve(FileNameTemplate template, Dictionary<string, string> metadata)
    {
        var pattern = template.Pattern;

        // <PrintJobName> - 打印作业名称
        pattern = pattern.Replace("<PrintJobName>",
            metadata.GetValueOrDefault("PrintJobName", template.DefaultPrefix));

        // <PrintJobAuthor> - 打印者
        pattern = pattern.Replace("<PrintJobAuthor>",
            metadata.GetValueOrDefault("PrintJobAuthor", Environment.UserName));

        // <PrintJobTime:format> - 打印作业时间
        pattern = Regex.Replace(pattern, @"<PrintJobTime:([^>]+)>", m =>
        {
            if (metadata.TryGetValue("PrintJobTime", out var timeStr) &&
                DateTime.TryParse(timeStr, out var dt))
                return dt.ToString(m.Groups[1].Value);
            return DateTime.Now.ToString(m.Groups[1].Value);
        });

        // <DateTime:format> - 当前日期时间
        pattern = Regex.Replace(pattern, @"<DateTime:([^>]+)>", m =>
            DateTime.Now.ToString(m.Groups[1].Value));

        // {format} 旧式日期占位符
        pattern = Regex.Replace(pattern, @"\{([^}]+)\}", m =>
            DateTime.Now.ToString(m.Groups[1].Value));

        // <format> 另一种语法
        pattern = Regex.Replace(pattern, @"<([^:>]+)>", m =>
        {
            // 检查是否是日期格式字符串
            try { return DateTime.Now.ToString(m.Groups[1].Value); }
            catch { return m.Value; }
        });

        // <Counter> - 自增计数
        if (pattern.Contains("<Counter>"))
        {
            var key = template.Pattern; // 每个模板独立计数
            _counters.TryGetValue(key, out var count);
            count++;
            _counters[key] = count;
            pattern = pattern.Replace("<Counter>", count.ToString("D4"));
        }

        // 清理非法文件名字符
        pattern = CleanFileName(pattern);

        // 如果结果为空，使用默认前缀
        if (string.IsNullOrWhiteSpace(pattern))
            pattern = $"{template.DefaultPrefix}_{DateTime.Now:yyyyMMdd_HHmmss}";

        return pattern;
    }

    /// <summary>
    /// 解析输出目录中的日期占位符
    /// </summary>
    public static string ResolveOutputRoot(string root, DateTime? date = null)
    {
        date ??= DateTime.Now;
        var result = Regex.Replace(root, @"\{([^}]+)\}", m => date.Value.ToString(m.Groups[1].Value));
        result = Regex.Replace(result, @"<([^>]+)>", m =>
        {
            try { return date.Value.ToString(m.Groups[1].Value); }
            catch { return m.Value; }
        });
        return result;
    }

    private static string CleanFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid)
            name = name.Replace(c, '_');
        return name.Trim();
    }
}
