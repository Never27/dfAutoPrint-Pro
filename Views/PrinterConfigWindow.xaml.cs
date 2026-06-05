using System.IO;
using System.Windows;
using PdfAutoPrint.Pro.Models;
using PdfAutoPrint.Pro.Services;

namespace PdfAutoPrint.Pro.Views;

public partial class PrinterConfigWindow : Window
{
    private readonly PrinterProfile _profile;
    private readonly LogService _log;

    public PrinterConfigWindow(PrinterProfile profile, LogService log)
    {
        InitializeComponent();
        _profile = profile;
        _log = log;
        LoadProfile();
        SetupEvents();
    }

    private void SetupEvents()
    {
        // 冲突处理模式切换
        rbIncrement.Checked += (s, e) =>
        {
            pnlIncrementSep.Visibility = Visibility.Visible;
            pnlBackupSuffix.Visibility = Visibility.Collapsed;
        };
        rbBackup.Checked += (s, e) =>
        {
            pnlIncrementSep.Visibility = Visibility.Collapsed;
            pnlBackupSuffix.Visibility = Visibility.Visible;
        };
        rbOverwrite.Checked += (s, e) =>
        {
            pnlIncrementSep.Visibility = Visibility.Collapsed;
            pnlBackupSuffix.Visibility = Visibility.Collapsed;
        };

        // 图片模式DPI显示
        rbImage.Checked += (s, e) => pnlImageDpi.Visibility = Visibility.Visible;
        rbOriginal.Checked += (s, e) => pnlImageDpi.Visibility = Visibility.Collapsed;

        // 水印按钮
        btnAddTextWatermark.Click += (s, e) => AddWatermark(WatermarkType.Text);
        btnAddImageWatermark.Click += (s, e) => AddWatermark(WatermarkType.Image);
        btnEditWatermark.Click += (s, e) => EditWatermark();
        btnRemoveWatermark.Click += (s, e) => RemoveWatermark();
    }

    private void LoadProfile()
    {
        // 输出设置
        chkAutoSave.IsChecked = _profile.Output.AutoSave;
        txtOutputRoot.Text = _profile.Output.OutputRoot;
        txtWatchFolder.Text = _profile.Output.WatchFolder;
        chkDeleteSource.IsChecked = _profile.Output.DeleteSourceAfterConvert;

        // 文件名
        txtFileNamePattern.Text = _profile.FileName.Pattern;
        txtDefaultPrefix.Text = _profile.FileName.DefaultPrefix;

        // 冲突处理
        switch (_profile.Conflict.Mode)
        {
            case ConflictMode.Overwrite: rbOverwrite.IsChecked = true; break;
            case ConflictMode.Increment: rbIncrement.IsChecked = true; break;
            case ConflictMode.Backup: rbBackup.IsChecked = true; break;
        }
        txtIncrementSep.Text = _profile.Conflict.IncrementSeparator;
        txtBackupSuffix.Text = _profile.Conflict.BackupSuffixFormat;

        // 质量
        if (_profile.Quality.Color == ColorMode.Color) rbColor.IsChecked = true;
        else rbGray.IsChecked = true;
        if (_profile.Quality.Mode == OutputMode.Original) rbOriginal.IsChecked = true;
        else rbImage.IsChecked = true;
        txtImageDpi.Text = _profile.Quality.ImageDpi.ToString();

        // 水印
        chkWatermarkEnabled.IsChecked = _profile.Watermark.Enabled;
        RefreshWatermarkList();

        // 通知
        chkSystemTray.IsChecked = _profile.Notification.EnableSystemTray;
        chkNotifySuccess.IsChecked = _profile.Notification.NotifyOnSuccess;
        chkNotifyFailure.IsChecked = _profile.Notification.NotifyOnFailure;

        // 服务
        txtPrinterName.Text = _profile.PrinterName;
        txtGsPath.Text = _profile.Service.GhostScriptPath ?? "";
        chkSkipOnFailure.IsChecked = _profile.Service.SkipOnFailure;
        chkEnabled.IsChecked = _profile.Enabled;
        // Log retention isn't per-printer, it's global - skip for now
    }

    private void SaveProfile()
    {
        _profile.PrinterName = txtPrinterName.Text;

        // 输出
        _profile.Output.AutoSave = chkAutoSave.IsChecked ?? true;
        _profile.Output.OutputRoot = txtOutputRoot.Text;
        _profile.Output.WatchFolder = txtWatchFolder.Text;
        _profile.Output.DeleteSourceAfterConvert = chkDeleteSource.IsChecked ?? true;

        // 文件名
        _profile.FileName.Pattern = txtFileNamePattern.Text;
        _profile.FileName.DefaultPrefix = txtDefaultPrefix.Text;

        // 冲突
        if (rbOverwrite.IsChecked == true) _profile.Conflict.Mode = ConflictMode.Overwrite;
        else if (rbIncrement.IsChecked == true) _profile.Conflict.Mode = ConflictMode.Increment;
        else _profile.Conflict.Mode = ConflictMode.Backup;
        _profile.Conflict.IncrementSeparator = txtIncrementSep.Text;
        _profile.Conflict.BackupSuffixFormat = txtBackupSuffix.Text;

        // 质量
        _profile.Quality.Color = rbColor.IsChecked == true ? ColorMode.Color : ColorMode.Grayscale;
        _profile.Quality.Mode = rbOriginal.IsChecked == true ? OutputMode.Original : OutputMode.Image;
        if (int.TryParse(txtImageDpi.Text, out var dpi)) _profile.Quality.ImageDpi = dpi;

        // 水印
        _profile.Watermark.Enabled = chkWatermarkEnabled.IsChecked ?? false;

        // 通知
        _profile.Notification.EnableSystemTray = chkSystemTray.IsChecked ?? true;
        _profile.Notification.NotifyOnSuccess = chkNotifySuccess.IsChecked ?? true;
        _profile.Notification.NotifyOnFailure = chkNotifyFailure.IsChecked ?? true;

        // 服务
        _profile.Service.GhostScriptPath = string.IsNullOrWhiteSpace(txtGsPath.Text) ? null : txtGsPath.Text;
        _profile.Service.SkipOnFailure = chkSkipOnFailure.IsChecked ?? true;
        _profile.Enabled = chkEnabled.IsChecked ?? true;

        // 更新端口名
        var safeName = _profile.PrinterName.Replace(" ", "_");
        _profile.PortName = $@"C:\PDFOutput\spool\{safeName}.prn";
    }

    private void RefreshWatermarkList()
    {
        lstWatermarks.Items.Clear();
        foreach (var item in _profile.Watermark.Items)
        {
            var desc = item.Type == WatermarkType.Text
                ? $"文字: {item.Text}"
                : $"图片: {Path.GetFileName(item.ImagePath ?? "")}";
            desc += $" | 位置: {item.Position.Anchor}";
            desc += $" | 透明度: {item.Opacity:P0}";
            lstWatermarks.Items.Add(desc);
        }
    }

    private void AddWatermark(WatermarkType type)
    {
        var item = new WatermarkItem
        {
            Type = type,
            Text = type == WatermarkType.Text ? "保密" : "",
            Position = new WatermarkPosition { Anchor = PositionAnchor.Center },
            Opacity = 0.3,
            PageRange = new PageRange { AllPages = true }
        };

        if (type == WatermarkType.Image)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp|所有文件|*.*",
                Title = "选择印章/签字图片"
            };
            if (dlg.ShowDialog() == true)
                item.ImagePath = dlg.FileName;
            else
                return;
        }

        _profile.Watermark.Items.Add(item);
        RefreshWatermarkList();
    }

    private void EditWatermark()
    {
        if (lstWatermarks.SelectedIndex < 0) return;
        var item = _profile.Watermark.Items[lstWatermarks.SelectedIndex];
        var editor = new WatermarkEditorWindow(item)
        {
            Owner = this
        };
        if (editor.ShowDialog() == true)
            RefreshWatermarkList();
    }

    private void RemoveWatermark()
    {
        if (lstWatermarks.SelectedIndex < 0) return;
        _profile.Watermark.Items.RemoveAt(lstWatermarks.SelectedIndex);
        RefreshWatermarkList();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        SaveProfile();
        _log.Info($"配置已保存: {_profile.PrinterName}");
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
