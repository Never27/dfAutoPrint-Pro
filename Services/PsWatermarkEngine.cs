using System.IO;
using System.Text.RegularExpressions;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Services;

/// <summary>
/// PostScript水印注入引擎：在PS转换为PDF前注入水印/印章/页码
/// </summary>
public class PsWatermarkEngine
{
    /// <summary>
    /// 将水印PostScript代码注入到源PS文件中
    /// </summary>
    public string InjectWatermarks(string psContent, WatermarkConfig config, string jobName = "")
    {
        if (!config.Enabled || config.Items.Count == 0)
            return psContent;

        // 在 %%EndComments 之后注入水印定义和页面包装
        var watermarkPs = BuildWatermarkPs(config, jobName);
        var lines = psContent.Split('\n').ToList();

        // 在 %%EndSetup 或 %%Page 注释之后注入
        int injectIndex = FindInjectionPoint(lines);

        if (injectIndex >= 0)
        {
            lines.Insert(injectIndex, watermarkPs);
        }
        else
        {
            // 如果找不到注入点，在文件末尾 %%EOF 之前注入
            var eofIndex = lines.FindLastIndex(l => l.Trim() == "%%EOF");
            if (eofIndex >= 0)
                lines.Insert(eofIndex, watermarkPs);
            else
                lines.Add(watermarkPs);
        }

        return string.Join("\n", lines);
    }

    private int FindInjectionPoint(List<string> lines)
    {
        // 优先在第一个页面开始之前
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith("%%Page:") || line == "%%BeginPageSetup")
            {
                return i + 1;
            }
        }

        // 其次在 %%EndSetup 之后
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == "%%EndSetup")
                return i + 1;
        }

        return -1;
    }

    private string BuildWatermarkPs(WatermarkConfig config, string jobName)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("% --- PdfAutoPrint Watermark Begin ---");
        sb.AppendLine("/pdfauto_watermark_dict 10 dict def");
        sb.AppendLine("pdfauto_watermark_dict begin");

        // 保存图形状态
        sb.AppendLine("gsave");

        foreach (var item in config.Items)
        {
            AppendWatermarkItem(sb, item, jobName);
        }

        sb.AppendLine("grestore");
        sb.AppendLine("end");
        sb.AppendLine("% --- PdfAutoPrint Watermark End ---");

        return sb.ToString();
    }

    private void AppendWatermarkItem(System.Text.StringBuilder sb, WatermarkItem item, string jobName)
    {
        var text = ResolvePlaceholders(item.Text, jobName);

        if (item.Type == WatermarkType.Text)
        {
            AppendTextWatermark(sb, item, text);
        }
        else if (item.Type == WatermarkType.Image && !string.IsNullOrEmpty(item.ImagePath))
        {
            AppendImageWatermark(sb, item);
        }
    }

    private void AppendTextWatermark(System.Text.StringBuilder sb, WatermarkItem item, string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var font = item.Font ?? new FontConfig();

        // 设置透明度和颜色
        sb.AppendLine($"gsave");

        // 设置字体
        var fontName = font.Family;
        var fontDef = $"/{fontName}";
        if (font.Bold && font.Italic) fontDef += "-BoldItalic";
        else if (font.Bold) fontDef += "-Bold";
        else if (font.Italic) fontDef += "-Italic";

        sb.AppendLine($"{fontDef} findfont {font.Size} scalefont setfont");

        // 设置颜色 (从hex转换)
        var (r, g, b) = ParseColor(font.Color);
        sb.AppendLine($"{r:F3} {g:F3} {b:F3} setrgbcolor");

        // 计算位置
        var (x, y) = CalculatePosition(item.Position, 612, 792);
        sb.AppendLine($"{x} {y} moveto");

        // 设置旋转
        if (Math.Abs(item.Rotation) > 0.01)
        {
            sb.AppendLine($"{x} {y} translate");
            sb.AppendLine($"{item.Rotation} rotate");
            sb.AppendLine($"0 0 moveto");
        }

        // 设置透明度 (通过overprint模拟)
        if (item.Opacity < 1.0)
        {
            sb.AppendLine($"/pdfauto_opacity {{ {item.Opacity:F2} .setopacityalpha }} def");
            sb.AppendLine("pdfauto_opacity");
        }

        // 显示文本 (需要转义PostScript特殊字符)
        var escaped = EscapePostScript(text);
        sb.AppendLine($"({escaped}) show");

        sb.AppendLine("grestore");
    }

    private void AppendImageWatermark(System.Text.StringBuilder sb, WatermarkItem item)
    {
        // PostScript图片嵌入（根据文件类型处理）
        // JPEG直接嵌入，其他格式需要先转换
        if (!File.Exists(item.ImagePath)) return;

        var ext = Path.GetExtension(item.ImagePath).ToLower();
        var (x, y) = CalculatePosition(item.Position, 612, 792);

        sb.AppendLine("gsave");
        sb.AppendLine($"{x} {y} translate");

        if (item.Rotation != 0)
            sb.AppendLine($"{item.Rotation} rotate");

        if (ext == ".jpg" || ext == ".jpeg")
        {
            // JPEG嵌入
            try
            {
                var jpegData = File.ReadAllBytes(item.ImagePath);
                var hex = Convert.ToHexString(jpegData);
                sb.AppendLine($"/pdfauto_jpgdata <{hex}> def");
                sb.AppendLine("pdfauto_jpgdata /DCTDecode filter");
                sb.AppendLine("pdfauto_jpgdata length string readstring pop");
            }
            catch
            {
                sb.AppendLine($"({Path.GetFileName(item.ImagePath)} - image load failed) show");
            }
        }
        else if (ext == ".eps")
        {
            // EPS直接include
            sb.AppendLine($"({item.ImagePath.Replace("\\", "/")}) run");
        }

        sb.AppendLine("grestore");
    }

    private static (double x, double y) CalculatePosition(WatermarkPosition pos, double pageWidth, double pageHeight)
    {
        // 将偏移量统一转换为Points
        double ox = pos.Unit switch
        {
            PositionUnit.Millimeters => pos.OffsetX * 2.83465,
            PositionUnit.Inches => pos.OffsetX * 72,
            _ => pos.OffsetX
        };
        double oy = pos.Unit switch
        {
            PositionUnit.Millimeters => pos.OffsetY * 2.83465,
            PositionUnit.Inches => pos.OffsetY * 72,
            _ => pos.OffsetY
        };

        return pos.Anchor switch
        {
            PositionAnchor.TopLeft => (ox, pageHeight - oy),
            PositionAnchor.TopCenter => (pageWidth / 2 + ox, pageHeight - oy),
            PositionAnchor.TopRight => (pageWidth - ox, pageHeight - oy),
            PositionAnchor.MiddleLeft => (ox, pageHeight / 2 + oy),
            PositionAnchor.Center => (pageWidth / 2 + ox, pageHeight / 2 + oy),
            PositionAnchor.MiddleRight => (pageWidth - ox, pageHeight / 2 + oy),
            PositionAnchor.BottomLeft => (ox, oy),
            PositionAnchor.BottomCenter => (pageWidth / 2 + ox, oy),
            PositionAnchor.BottomRight => (pageWidth - ox, oy),
            PositionAnchor.Absolute => (ox, pageHeight - oy),
            _ => (pageWidth / 2, pageHeight / 2)
        };
    }

    private static string ResolvePlaceholders(string text, string jobName)
    {
        return text
            .Replace("<PrintJobName>", jobName)
            .Replace("<DateTime:" + DateTime.Now.ToString("yyyyMMdd") + ">", DateTime.Now.ToString("yyyyMMdd"))
            .Replace("<PageNum>", "?")
            .Replace("<TotalPages>", "?");
    }

    // 用 Regex 替换 DateTime 占位符
    public static string ResolveDateTimePlaceholders(string text)
    {
        return Regex.Replace(text, @"<DateTime:([^>]+)>", m =>
            DateTime.Now.ToString(m.Groups[1].Value));
    }

    private static string EscapePostScript(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static (double r, double g, double b) ParseColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return (
                Convert.ToInt32(hex[..2], 16) / 255.0,
                Convert.ToInt32(hex[2..4], 16) / 255.0,
                Convert.ToInt32(hex[4..6], 16) / 255.0
            );
        }
        return (1, 0, 0); // 默认红色
    }
}
