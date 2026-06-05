using System.Drawing;
using System.Windows;
using PdfAutoPrint.Pro.Views;
using WinForms = System.Windows.Forms;

namespace PdfAutoPrint.Pro;

public partial class App : Application
{
    private WinForms.NotifyIcon? _notifyIcon;
    private bool _isExiting;

    /// <summary>静态托盘图标引用，供 ViewModel 弹出气泡通知</summary>
    public static WinForms.NotifyIcon? TrayIcon { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 应用退出时不自动关闭（由托盘菜单控制）
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 加载配置，检查是否启用托盘
        var configService = new Services.ConfigService();
        configService.Load();

        var trayEnabled = configService.Config.Printers.Any(p => p.Notification.EnableSystemTray);

        if (trayEnabled)
        {
            CreateTrayIcon();
        }

        // 手动创建主窗口
        var mainWindow = new MainWindow();
        mainWindow.Closing += MainWindow_Closing;
        MainWindow = mainWindow;
        mainWindow.Show();

        // 开机自启
        if (configService.Config.AutoStart)
        {
            Services.StartupService.SetAutoStart(true);
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isExiting) return;

        // 检查是否有打印机启用了托盘
        var anyTray = false;
        try
        {
            var svc = new Services.ConfigService();
            svc.Load();
            anyTray = svc.Config.Printers.Any(p => p.Notification.EnableSystemTray);
        }
        catch { /* 配置读取失败则直接退出 */ }

        if (anyTray && _notifyIcon != null)
        {
            e.Cancel = true;
            MainWindow?.Hide();
            _notifyIcon.ShowBalloonTip(3000, "PdfAutoPrint Pro", "程序已最小化到系统托盘", WinForms.ToolTipIcon.Info);
        }
    }

    private void CreateTrayIcon()
    {
        // 使用应用图标（Assets/app.ico）
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
        var icon = System.IO.File.Exists(iconPath)
            ? new System.Drawing.Icon(iconPath)
            : CreateDefaultIcon();

        _notifyIcon = new WinForms.NotifyIcon
        {
            Icon = icon,
            Visible = true,
            Text = "PdfAutoPrint Pro"
        };

        // 右键菜单
        var contextMenu = new WinForms.ContextMenuStrip();
        contextMenu.Items.Add("显示主窗口", null, OnShowWindow);
        contextMenu.Items.Add(new WinForms.ToolStripSeparator());
        contextMenu.Items.Add("退出", null, OnExit);
        _notifyIcon.ContextMenuStrip = contextMenu;

        // 双击恢复窗口
        _notifyIcon.DoubleClick += OnShowWindow;

        TrayIcon = _notifyIcon;
    }

    private void OnShowWindow(object? sender, EventArgs e)
    {
        if (MainWindow != null)
        {
            MainWindow.Show();
            MainWindow.WindowState = WindowState.Normal;
            MainWindow.Activate();
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        TrayIcon = null;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isExiting = true;
        _notifyIcon?.Dispose();
        TrayIcon = null;
        base.OnExit(e);
    }

    /// <summary>
    /// 生成默认托盘图标（纯色圆形 + P 字母）
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        var size = 32;
        using var bitmap = new Bitmap(size, size);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // 背景圆
        using var bgBrush = new SolidBrush(Color.FromArgb(83, 74, 183));
        g.FillEllipse(bgBrush, 1, 1, size - 2, size - 2);

        // 字母 P
        using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.White);
        var textSize = g.MeasureString("P", font);
        g.DrawString("P", font, textBrush,
            (size - textSize.Width) / 2,
            (size - textSize.Height) / 2 - 1);

        var icon = Icon.FromHandle(bitmap.GetHicon());
        return (Icon)icon.Clone();
    }
}
