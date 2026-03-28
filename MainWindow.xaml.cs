using System.Windows;
using System.Windows.Input;
using Pam.ViewModels;

namespace Pam;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Start in bottom-right corner of primary screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;

        Loaded += (_, _) => _viewModel.Start();
        Closed += (_, _) => _viewModel.Stop();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _ = _viewModel.RefreshAll();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
