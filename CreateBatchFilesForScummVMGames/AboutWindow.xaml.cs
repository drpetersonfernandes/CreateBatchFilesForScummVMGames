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
        var version = typeof(App).Assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement element)
            {
                element.IsEnabled = false;
            }

            var service = App.GitHubReleaseService;
            if (service == null)
            {
                MessageBox.Show(this, "Update service is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var releaseInfo = await service.CheckForUpdateAsync();

            if (releaseInfo is { IsNewVersionAvailable: true, LatestVersion: not null, ReleaseUrl: not null })
            {
                var result = MessageBox.Show(this,
                    $"A new version ({releaseInfo.LatestVersion}) is available!\n\nWould you like to open the release page?",
                    "Update Available", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(releaseInfo.ReleaseUrl) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        _ = App.SendBugReportAsync($"Error opening URL: {releaseInfo.ReleaseUrl}", ex);
                        MessageBox.Show(this, $"Unable to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                var currentVersion = GetApplicationVersion();
                MessageBox.Show(this,
                    $"You are running the latest version ({currentVersion}).",
                    "No Updates Available", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _ = App.SendBugReportAsync("Failed to check for updates from About window", ex);
            MessageBox.Show(this, $"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (sender is FrameworkElement element)
            {
                element.IsEnabled = true;
            }
        }
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