using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Pam.ViewModels;
using Pam.Views;

namespace Pam;

public partial class MainWindow : Window
{
    private readonly MainViewModel _claudeUsageVm;
    private readonly LandingView _landingView;
    private readonly ClaudeUsageView _claudeUsageView;

    private static readonly List<AppEntry> Apps =
    [
        new() { Id = "claude-usage", Name = "Claude Usage", Description = "5-hour and 7-day usage monitoring" },
    ];

    public MainWindow()
    {
        InitializeComponent();

        _claudeUsageVm = new MainViewModel();

        _landingView = new LandingView
        {
            DataContext = new LandingViewModel { Apps = Apps }
        };

        _claudeUsageView = new ClaudeUsageView
        {
            DataContext = _claudeUsageVm
        };

        // Start in bottom-right corner of primary screen
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;

        Loaded += (_, _) =>
        {
            _claudeUsageVm.Start();
            NavigateTo("home");
        };
        Closed += (_, _) => _claudeUsageVm.Stop();
    }

    public void NavigateTo(string id)
    {
        ContentArea.Content = id switch
        {
            "claude-usage" => _claudeUsageView,
            _ => _landingView,
        };
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void Home_Click(object sender, RoutedEventArgs e) => NavigateTo("home");
    private void ClaudeUsage_Click(object sender, RoutedEventArgs e) => NavigateTo("claude-usage");
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}

public class LandingViewModel
{
    public List<AppEntry> Apps { get; init; } = [];
}
