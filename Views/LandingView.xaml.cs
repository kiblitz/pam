using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Pam.Views;

public partial class LandingView : UserControl
{
    public LandingView()
    {
        InitializeComponent();
    }

    private void AppItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AppEntry entry })
        {
            var mainWindow = (MainWindow)Window.GetWindow(this)!;
            mainWindow.NavigateTo(entry.Id);
        }
    }
}

public class AppEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
}
