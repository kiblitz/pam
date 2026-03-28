using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pam.ViewModels;

namespace Pam.Views;

public partial class ScreenshotView : UserControl
{
    private static readonly SolidColorBrush ValidBorder = new(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush InvalidBorder = new(Color.FromArgb(0xAA, 0xFF, 0x44, 0x44));

    public ScreenshotView()
    {
        InitializeComponent();
    }

    private ScreenshotViewModel Vm => (ScreenshotViewModel)DataContext;

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Window.GetWindow(this)!;
        mainWindow.NavigateTo("home");
    }

    private void DelayInput_GotFocus(object sender, RoutedEventArgs e)
    {
        DelayInput.SelectAll();
    }

    private void DelayInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext == null) return;

        var text = DelayInput.Text.Trim();
        if (int.TryParse(text, out var seconds) && seconds >= 0 && seconds <= 60)
        {
            Vm.DelaySeconds = seconds;
            DelayInput.BorderBrush = ValidBorder;
        }
        else
        {
            DelayInput.BorderBrush = InvalidBorder;
        }
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;
        _ = Vm.CaptureScreenshot(window);
    }

    private void Record_Click(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;
        _ = Vm.StartOrStopRecording(window);
    }
}
