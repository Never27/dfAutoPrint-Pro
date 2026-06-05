using System.Windows;
using System.Windows.Controls;
using PdfAutoPrint.Pro.Models;

namespace PdfAutoPrint.Pro.Views;

public partial class WatermarkEditorWindow : Window
{
    private readonly WatermarkItem _item;

    public WatermarkEditorWindow(WatermarkItem item)
    {
        InitializeComponent();
        _item = item;
        LoadItem();
        SetupEvents();
    }

    private void SetupEvents()
    {
        if (_item.Type == WatermarkType.Text)
        {
            txtType.Text = "文字水印";
            pnlText.Visibility = Visibility.Visible;
            pnlImage.Visibility = Visibility.Collapsed;
        }
        else
        {
            txtType.Text = "图片印章";
            pnlText.Visibility = Visibility.Collapsed;
            pnlImage.Visibility = Visibility.Visible;
        }

        btnBrowseImage.Click += (s, e) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.eps|所有文件|*.*",
                Title = "选择图片"
            };
            if (dlg.ShowDialog() == true)
            {
                txtImagePath.Text = dlg.FileName;
            }
        };
    }

    private void LoadItem()
    {
        // 文本水印
        txtContent.Text = _item.Text;
        if (_item.Font != null)
        {
            txtFontFamily.Text = _item.Font.Family;
            txtFontSize.Text = _item.Font.Size.ToString();
            txtFontColor.Text = _item.Font.Color;
            chkBold.IsChecked = _item.Font.Bold;
            chkItalic.IsChecked = _item.Font.Italic;
        }

        // 图片水印
        txtImagePath.Text = _item.ImagePath ?? "";
        txtImageScale.Text = _item.ImageScalePercent > 0 ? _item.ImageScalePercent.ToString() : "100";

        // 位置
        foreach (ComboBoxItem cbi in cmbAnchor.Items)
        {
            if (cbi.Tag?.ToString() == _item.Position.Anchor.ToString())
            {
                cmbAnchor.SelectedItem = cbi;
                break;
            }
        }
        txtOffsetX.Text = _item.Position.OffsetX.ToString("F1");
        txtOffsetY.Text = _item.Position.OffsetY.ToString("F1");
        cmbUnit.SelectedIndex = _item.Position.Unit switch
        {
            PositionUnit.Points => 0,
            PositionUnit.Millimeters => 1,
            PositionUnit.Inches => 2,
            _ => 0
        };

        // 其他
        txtOpacity.Text = _item.Opacity.ToString("F2");
        txtRotation.Text = _item.Rotation.ToString("F1");

        // 页面范围
        if (_item.PageRange != null)
        {
            chkAllPages.IsChecked = _item.PageRange.AllPages;
            txtSkipFirst.Text = _item.PageRange.SkipFirst.ToString();
            txtSkipLast.Text = _item.PageRange.SkipLast.ToString();
        }
    }

    private void SaveItem()
    {
        _item.Text = txtContent.Text;

        _item.Font = new FontConfig
        {
            Family = txtFontFamily.Text,
            Size = double.TryParse(txtFontSize.Text, out var fs) ? fs : 48,
            Color = txtFontColor.Text,
            Bold = chkBold.IsChecked ?? false,
            Italic = chkItalic.IsChecked ?? false
        };

        _item.ImagePath = txtImagePath.Text;
        _item.ImageScalePercent = double.TryParse(txtImageScale.Text, out var sc) ? sc : 100;

        if (cmbAnchor.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
        {
            _item.Position.Anchor = Enum.Parse<PositionAnchor>(cbi.Tag.ToString()!);
        }
        _item.Position.OffsetX = double.TryParse(txtOffsetX.Text, out var ox) ? ox : 0;
        _item.Position.OffsetY = double.TryParse(txtOffsetY.Text, out var oy) ? oy : 0;
        _item.Position.Unit = cmbUnit.SelectedIndex switch
        {
            0 => PositionUnit.Points,
            1 => PositionUnit.Millimeters,
            2 => PositionUnit.Inches,
            _ => PositionUnit.Points
        };

        _item.Opacity = double.TryParse(txtOpacity.Text, out var op) ? Math.Clamp(op, 0, 1) : 0.3;
        _item.Rotation = double.TryParse(txtRotation.Text, out var rot) ? rot : 0;

        _item.PageRange = new Models.PageRange
        {
            AllPages = chkAllPages.IsChecked ?? true,
            SkipFirst = int.TryParse(txtSkipFirst.Text, out var sf) ? sf : 0,
            SkipLast = int.TryParse(txtSkipLast.Text, out var sl) ? sl : 0
        };
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        SaveItem();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
