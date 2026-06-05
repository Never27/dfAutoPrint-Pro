namespace PdfAutoPrint.Pro.Models;

/// <summary>
/// 水印/印章/页码叠加总配置
/// </summary>
public class WatermarkConfig
{
    /// <summary>是否启用水印功能</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>水印项列表（支持多个水印叠加）</summary>
    public List<WatermarkItem> Items { get; set; } = new();
}

/// <summary>
/// 单个水印项
/// </summary>
public class WatermarkItem
{
    /// <summary>水印类型</summary>
    public WatermarkType Type { get; set; } = WatermarkType.Text;

    /// <summary>
    /// 文本内容（Type=Text时使用，支持占位符）
    /// 支持: &lt;PageNum&gt; &lt;TotalPages&gt; &lt;DateTime:format&gt; 等
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>图片路径（Type=Image时使用）</summary>
    public string? ImagePath { get; set; }

    /// <summary>位置配置</summary>
    public WatermarkPosition Position { get; set; } = new();

    /// <summary>透明度 0.0~1.0，1.0=完全不透明</summary>
    public double Opacity { get; set; } = 0.3;

    /// <summary>旋转角度（度数，0=不旋转）</summary>
    public double Rotation { get; set; } = 0;

    /// <summary>页面范围</summary>
    public PageRange? PageRange { get; set; }

    /// <summary>文字水印专用字体配置</summary>
    public FontConfig? Font { get; set; }

    /// <summary>图片水印专用缩放，0=自动适配页面</summary>
    public double ImageScalePercent { get; set; } = 0;
}

public enum WatermarkType
{
    /// <summary>文字水印</summary>
    Text,
    /// <summary>图片水印（印章、签字、Logo）</summary>
    Image
}

/// <summary>
/// 水印绝对位置
/// </summary>
public class WatermarkPosition
{
    /// <summary>定位锚点</summary>
    public PositionAnchor Anchor { get; set; } = PositionAnchor.Center;

    /// <summary>距左边的偏移量（单位：Points，1pt≈0.353mm）</summary>
    public double OffsetX { get; set; } = 0;

    /// <summary>距顶部的偏移量</summary>
    public double OffsetY { get; set; } = 0;

    /// <summary>偏移量单位</summary>
    public PositionUnit Unit { get; set; } = PositionUnit.Points;
}

public enum PositionAnchor
{
    TopLeft, TopCenter, TopRight,
    MiddleLeft, Center, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
    /// <summary>使用绝对坐标（OffsetX=距左，OffsetY=距顶）</summary>
    Absolute
}

public enum PositionUnit
{
    Points,
    Millimeters,
    Inches
}

/// <summary>
/// 页面范围控制
/// </summary>
public class PageRange
{
    /// <summary>是否所有页面</summary>
    public bool AllPages { get; set; } = true;

    /// <summary>起始页（1-based），null=第一页</summary>
    public int? FirstPage { get; set; }

    /// <summary>结束页，null=最后一页</summary>
    public int? LastPage { get; set; }

    /// <summary>跳过前N页（如跳过封面）</summary>
    public int SkipFirst { get; set; } = 0;

    /// <summary>跳过后N页</summary>
    public int SkipLast { get; set; } = 0;
}

/// <summary>
/// 字体配置
/// </summary>
public class FontConfig
{
    public string Family { get; set; } = "SimSun";
    public double Size { get; set; } = 48;
    public bool Bold { get; set; } = false;
    public bool Italic { get; set; } = false;
    /// <summary>颜色 (RGB hex)</summary>
    public string Color { get; set; } = "#FF0000";

    public override string ToString()
    {
        var style = "";
        if (Bold) style += "-Bold";
        if (Italic) style += "-Italic";
        return $"/{Family}{style} findfont {Size} scalefont setfont";
    }
}
