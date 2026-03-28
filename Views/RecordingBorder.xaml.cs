using System.Windows;

namespace Pam.Views;

public partial class RecordingBorder : Window
{
    public RecordingBorder(Rect region)
    {
        InitializeComponent();

        // Add padding so the border sits outside the recorded region
        Left = region.X - 3;
        Top = region.Y - 3;
        Width = region.Width + 6;
        Height = region.Height + 6;
    }
}
