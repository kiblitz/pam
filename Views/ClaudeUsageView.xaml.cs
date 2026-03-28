using System.Windows;
using System.Windows.Controls;

namespace Pam.Views;

public partial class ClaudeUsageView : UserControl
{
    public ClaudeUsageView()
    {
        InitializeComponent();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        var mainWindow = (MainWindow)Window.GetWindow(this)!;
        mainWindow.NavigateTo("home");
    }
}
