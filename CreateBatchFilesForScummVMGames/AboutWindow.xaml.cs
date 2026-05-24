using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CreateBatchFilesForScummVMGames;

public partial class AboutWindow
{
    public AboutWindow()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        AppVersionTextBlock.Text = $"Version: {GetApplicationVersion()}";
    }

    private static string GetApplicationVersion()
    {
        var version = typeof(AboutWindow).Assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            _ = App.SendBugReportAsync($"Error opening URL: {e.Uri.AbsoluteUri}", ex);

            // Notify user
            MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}