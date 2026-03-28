using System.Windows;
using System.Windows.Controls;
using Pam.ViewModels;

namespace Pam.Views;

public partial class ScreenshotView : UserControl
{
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

    private void Delay_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag && int.TryParse(tag, out var seconds))
            Vm.DelaySeconds = seconds;
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
