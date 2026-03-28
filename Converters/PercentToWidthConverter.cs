using System;
using System.Globalization;
using System.Windows.Data;

namespace Pam.Converters;

/// <summary>
/// Converts a percentage (0-100) and a container width to a pixel width.
/// values[0] = percentage (double), values[1] = container width (double).
/// </summary>
public class PercentToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2)
            return 0.0;

        var percent = values[0] is double p ? p : 0.0;
        var containerWidth = values[1] is double w ? w : 0.0;

        return Math.Max(0, Math.Min(containerWidth, containerWidth * percent / 100.0));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
