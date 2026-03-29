using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Pam.ViewModels;

namespace Pam.Views;

public partial class ScreenshotView : UserControl
{
    private static readonly SolidColorBrush ValidBorder = new(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF));
    private static readonly SolidColorBrush InvalidBorder = new(Color.FromArgb(0xAA, 0xFF, 0x44, 0x44));
    private bool _delayValid = true;
    private bool _fpsValid = true;

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

    private void DelayInput_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!DelayInput.IsKeyboardFocusWithin)
        {
            DelayInput.Focus();
            e.Handled = true;
        }
    }

    private void DelayInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext == null) return;

        var text = DelayInput.Text.Trim();
        if (int.TryParse(text, out var seconds) && seconds >= 0 && seconds <= 60)
        {
            Vm.DelaySeconds = seconds;
            DelayInput.BorderBrush = ValidBorder;
            _delayValid = true;
        }
        else
        {
            DelayInput.BorderBrush = InvalidBorder;
            _delayValid = false;
        }
    }

    private void FpsInput_GotFocus(object sender, RoutedEventArgs e)
    {
        FpsInput.SelectAll();
    }

    private void FpsInput_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (!FpsInput.IsKeyboardFocusWithin)
        {
            FpsInput.Focus();
            e.Handled = true;
        }
    }

    private void FpsInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext == null) return;

        var text = FpsInput.Text.Trim();
        if (int.TryParse(text, out var fps) && fps >= 1 && fps <= 120)
        {
            Vm.Fps = fps;
            FpsInput.BorderBrush = ValidBorder;
            _fpsValid = true;
        }
        else
        {
            FpsInput.BorderBrush = InvalidBorder;
            _fpsValid = false;
        }
    }

    private void Capture_Click(object sender, RoutedEventArgs e)
    {
        if (!_delayValid) return;
        var window = Window.GetWindow(this)!;
        _ = Vm.CaptureScreenshot(window);
    }

    private void Record_Click(object sender, RoutedEventArgs e)
    {
        if ((!_delayValid || !_fpsValid) && !Vm.IsRecording) return;
        var window = Window.GetWindow(this)!;
        _ = Vm.StartOrStopRecording(window);
    }
}
