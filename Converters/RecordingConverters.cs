using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Pam.Converters;

public class RecordingBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isRecording = value is true;
        return isRecording
            ? new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0x44, 0x44))
            : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class RecordingTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? "Stop" : "Record";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
