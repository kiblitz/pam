using System.Windows;
using Pam.Services;

namespace Pam.Views;

public partial class RecordingBorder : Window
{
    /// <summary>
    /// Takes a region in physical screen pixels (from PointToScreen)
    /// and positions this window correctly accounting for DPI scaling.
    /// </summary>
    public RecordingBorder(Rect physicalRegion)
    {
        InitializeComponent();

        var logical = DpiHelper.PhysicalToLogical(physicalRegion);

        Left = logical.X - 3;
        Top = logical.Y - 3;
        Width = logical.Width + 6;
        Height = logical.Height + 6;
    }
}
