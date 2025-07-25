using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace CreateBatchFilesForScummVMGames;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();

        LogMessage("Welcome to the Batch File Creator for ScummVM Games.");
        LogMessage("");
        LogMessage("This program creates batch files to launch your ScummVM games.");
        LogMessage("Please follow these steps:");
        LogMessage("1. Select the ScummVM executable file (scummvm.exe)");
        LogMessage("2. Select the root folder containing your ScummVM game folders");
        LogMessage("3. Click 'Create Batch Files' to generate the batch files");
        LogMessage("");
        UpdateStatusBarMessage("Ready");
    }

    private void UpdateStatusBarMessage(string message)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void LogMessage(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        });
    }

    private void BrowseScummVMButton_Click(object sender, RoutedEventArgs e)
    {
        var scummvmExePath = SelectFile();
        if (string.IsNullOrEmpty(scummvmExePath)) return;

        ScummVmPathTextBox.Text = scummvmExePath;
        LogMessage($"ScummVM executable selected: {scummvmExePath}");
        UpdateStatusBarMessage("ScummVM executable selected.");

        if (scummvmExePath.EndsWith("scummvm.exe", StringComparison.OrdinalIgnoreCase)) return;

        LogMessage("Warning: The selected file does not appear to be scummvm.exe.");
        _ = ReportBugAsync("User selected a file that doesn't appear to be scummvm.exe: " + scummvmExePath);
    }

    private void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var rootFolder = SelectFolder();
        if (string.IsNullOrEmpty(rootFolder)) return;

        GameFolderTextBox.Text = rootFolder;
        LogMessage($"Game folder selected: {rootFolder}");
        UpdateStatusBarMessage("Game folder selected.");
    }

    private async void CreateBatchFilesButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var scummvmExePath = ScummVmPathTextBox.Text;
            var rootFolder = GameFolderTextBox.Text;

            if (string.IsNullOrEmpty(scummvmExePath))
            {
                LogMessage("Error: No ScummVM executable selected.");
                ShowError("Please select the ScummVM executable file (scummvm.exe).");
                UpdateStatusBarMessage("Error: ScummVM executable not selected.");
                return;
            }

            if (!File.Exists(scummvmExePath))
            {
                LogMessage($"Error: ScummVM executable not found at path: {scummvmExePath}");
                ShowError("The selected ScummVM executable file does not exist.");
                await ReportBugAsync("ScummVM executable not found", new FileNotFoundException("The ScummVM executable was not found", scummvmExePath));
                UpdateStatusBarMessage("Error: ScummVM executable not found.");
                return;
            }

            if (string.IsNullOrEmpty(rootFolder))
            {
                LogMessage("Error: No game folder selected.");
                ShowError("Please select the root folder containing your ScummVM game folders.");
                UpdateStatusBarMessage("Error: Game folder not selected.");
                return;
            }

            if (!Directory.Exists(rootFolder))
            {
                LogMessage($"Error: Game folder not found at path: {rootFolder}");
                ShowError("The selected game folder does not exist.");
                await ReportBugAsync("Game folder not found", new DirectoryNotFoundException($"Game folder not found: {rootFolder}"));
                UpdateStatusBarMessage("Error: Game folder not found.");
                return;
            }

            try
            {
                CreateBatchFilesForScummVmGames(rootFolder, scummvmExePath);
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating batch files: {ex.Message}");
                ShowError($"An error occurred while creating batch files: {ex.Message}");
                await ReportBugAsync("Error creating batch files", ex);
                UpdateStatusBarMessage("Process failed with an error.");
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error creating batch files", ex);
            UpdateStatusBarMessage("An unexpected error occurred.");
        }
    }

    private static string? SelectFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Please select the root folder where your ScummVM game folders are located."
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    private string? SelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Please select the ScummVM executable file (scummvm.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private void CreateBatchFilesForScummVmGames(string rootFolder, string scummvmExePath)
    {
        try
        {
            var gameDirectories = Directory.GetDirectories(rootFolder);
            var filesCreated = 0;

            LogMessage("");
            LogMessage("Starting batch file creation process...");
            UpdateStatusBarMessage("Creating batch files...");

            foreach (var gameDirectory in gameDirectories)
            {
                try
                {
                    var gameFolderName = Path.GetFileName(gameDirectory);
                    var batchFilePath = Path.Combine(rootFolder, gameFolderName + ".bat");

                    using (StreamWriter sw = new(batchFilePath))
                    {
                        sw.WriteLine($"\"{scummvmExePath}\" -p \"{gameDirectory}\" --auto-detect --fullscreen");
                        LogMessage($"Batch file created: {batchFilePath}");
                    }

                    filesCreated++;
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating batch file for {gameDirectory}: {ex.Message}");
                    _ = ReportBugAsync($"Error creating batch file for {Path.GetFileName(gameDirectory)}", ex);
                }
            }

            if (filesCreated > 0)
            {
                LogMessage("");
                LogMessage($"{filesCreated} batch files have been successfully created.");
                LogMessage("They are located in the root folder of your ScummVM games.");
                UpdateStatusBarMessage($"{filesCreated} batch files created successfully.");

                ShowMessageBox($"{filesCreated} batch files have been successfully created.\n\n" +
                               "They are located in the root folder of your ScummVM games.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LogMessage("No game folders found. No batch files were created.");
                ShowError("No game folders found. No batch files were created.");
                UpdateStatusBarMessage("No game folders found. No files were created.");
                _ = ReportBugAsync("No game folders found",
                    new DirectoryNotFoundException("No subdirectories found in the game folder"));
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error accessing folder structure: {ex.Message}");
            UpdateStatusBarMessage("Error accessing folder structure.");
            _ = ReportBugAsync("Error accessing folder structure during batch file creation", ex);
            throw;
        }
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        Dispatcher.Invoke(() =>
            MessageBox.Show(this, message, title, buttons, icon));
    }

    private void ShowError(string message)
    {
        ShowMessageBox(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task ReportBugAsync(string message, Exception? exception = null)
    {
        try
        {
            var fullReport = new StringBuilder();
            var assemblyName = GetType().Assembly.GetName();

            // Add system information
            fullReport.AppendLine("=== Bug Report ===");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Application: {assemblyName.Name}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Version: {assemblyName.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"OS: {Environment.OSVersion}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $".NET Version: {Environment.Version}");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Date/Time: {DateTime.Now}");
            fullReport.AppendLine();

            // Add a message
            fullReport.AppendLine("=== Error Message ===");
            fullReport.AppendLine(message);
            fullReport.AppendLine();

            // Add exception details if available
            if (exception != null)
            {
                fullReport.AppendLine("=== Exception Details ===");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.GetType().FullName}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.Message}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Source: {exception.Source}");
                fullReport.AppendLine("Stack Trace:");
                fullReport.AppendLine(exception.StackTrace);

                // Add inner exception if available
                if (exception.InnerException != null)
                {
                    fullReport.AppendLine("Inner Exception:");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Type: {exception.InnerException.GetType().FullName}");
                    fullReport.AppendLine(CultureInfo.InvariantCulture, $"Message: {exception.InnerException.Message}");
                    fullReport.AppendLine("Stack Trace:");
                    fullReport.AppendLine(exception.InnerException.StackTrace);
                }
            }

            // Add log contents if available
            if (LogTextBox != null)
            {
                var logContent = string.Empty;

                // Safely get log content from UI thread
                await Dispatcher.InvokeAsync(() =>
                {
                    logContent = LogTextBox.Text;
                });

                if (!string.IsNullOrEmpty(logContent))
                {
                    fullReport.AppendLine();
                    fullReport.AppendLine("=== Application Log ===");
                    fullReport.Append(logContent);
                }
            }

            // Add ScummVM and games folder paths if available
            if (ScummVmPathTextBox != null && GameFolderTextBox != null)
            {
                var scummvmPath = string.Empty;
                var gameFolderPath = string.Empty;

                await Dispatcher.InvokeAsync(() =>
                {
                    scummvmPath = ScummVmPathTextBox.Text;
                    gameFolderPath = GameFolderTextBox.Text;
                });

                fullReport.AppendLine();
                fullReport.AppendLine("=== Configuration ===");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"ScummVM Path: {scummvmPath}");
                fullReport.AppendLine(CultureInfo.InvariantCulture, $"Games Folder: {gameFolderPath}");
            }

            // Silently send the report using the shared service from the App class
            if (App.BugReportService != null)
            {
                await App.BugReportService.SendBugReportAsync(fullReport.ToString());
            }
        }
        catch
        {
            // Silently fail if error reporting itself fails
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var aboutWindow = new AboutWindow();
            aboutWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            LogMessage($"Error opening About window: {ex.Message}");
            _ = ReportBugAsync("Error opening About window", ex);
        }
    }
}
