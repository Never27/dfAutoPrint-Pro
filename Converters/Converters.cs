using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PdfAutoPrint.Pro.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var boolValue = value is bool b && b;
        if (parameter is string p && p == "Invert")
            boolValue = !boolValue;
        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && !b;
}

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Success" => "#3B6D11",
            "Failed" => "#A32D2D",
            "Skipped" => "#854F0B",
            _ => "#666666"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FileSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }
        return "0 B";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
