using System.Windows;
using System.Windows.Media;

namespace Pam.Services;

public static class DpiHelper
{
    /// <summary>
    /// Gets the DPI scale factor for the primary monitor.
    /// Returns (scaleX, scaleY) where 1.0 = 96 DPI (100% scaling).
    /// </summary>
    public static (double X, double Y) GetScale()
    {
        var source = PresentationSource.FromVisual(Application.Current.MainWindow);
        if (source?.CompositionTarget != null)
        {
            var m = source.CompositionTarget.TransformToDevice;
            return (m.M11, m.M22);
        }
        return (1.0, 1.0);
    }

    /// <summary>
    /// Converts physical screen pixels to WPF logical units.
    /// </summary>
    public static Rect PhysicalToLogical(Rect physical)
    {
        var (sx, sy) = GetScale();
        return new Rect(
            physical.X / sx,
            physical.Y / sy,
            physical.Width / sx,
            physical.Height / sy);
    }
}
