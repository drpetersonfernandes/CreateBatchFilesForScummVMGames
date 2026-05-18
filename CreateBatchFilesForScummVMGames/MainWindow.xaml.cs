using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CreateBatchFilesForScummVMGames.Services;
using Microsoft.Win32;

namespace CreateBatchFilesForScummVMGames;

public partial class MainWindow
{
    private CancellationTokenSource? _cts;

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
        Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            StatusBarMessage.Text = message;
        });
    }

    private void LogMessage(string message)
    {
        Application.Current?.Dispatcher.InvokeAsync(() =>
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

    private async void CreateBatchFilesButton_ClickAsync(object sender, RoutedEventArgs e)
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
                var outputFormat = CreateBatRadioButton.IsChecked == true ? "bat"
                    : CreateSimpleScummVmRadioButton.IsChecked == true ? "simple"
                    : CreateRocknixScummVmRadioButton.IsChecked == true ? "rocknix"
                    : "bat";

                CreateBatchFilesButton.IsEnabled = false;
                CreateBatchFilesButton.Content = "Processing...";
                CancelButton.Visibility = Visibility.Visible;
                Mouse.OverrideCursor = Cursors.Wait;

                _cts = new CancellationTokenSource();
                var ct = _cts.Token;

                await Task.Run(() => CreateBatchFilesForScummVmGames(rootFolder, scummvmExePath, outputFormat, ct), ct);
            }
            catch (OperationCanceledException)
            {
                LogMessage("");
                LogMessage("Process cancelled by user.");
                UpdateStatusBarMessage("Cancelled.");
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating files: {ex.Message}");
                ShowError($"An error occurred while creating files: {ex.Message}");
                await ReportBugAsync("Error creating files", ex);
                UpdateStatusBarMessage("Process failed with an error.");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                CreateBatchFilesButton.IsEnabled = true;
                CreateBatchFilesButton.Content = "Create Files";
                CancelButton.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
            }
        }
        catch (Exception ex)
        {
            await ReportBugAsync("Error creating files", ex);
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

    private static string? SelectFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Please select the ScummVM executable file (scummvm.exe)",
            Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*",
            RestoreDirectory = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    private static List<string> GetGameFolderCandidates(string rootFolder)
    {
        var result = new List<string>();

        foreach (var firstLevelDir in Directory.EnumerateDirectories(rootFolder))
        {
            var gameFolder = FindGameFolder(firstLevelDir);
            if (gameFolder != null)
                result.Add(gameFolder);
        }

        return result;
    }

    private static string? FindGameFolder(string directory)
    {
        if (Directory.EnumerateFiles(directory).Any())
            return directory;

        foreach (var subDir in Directory.EnumerateDirectories(directory))
        {
            var result = FindGameFolder(subDir);
            if (result != null)
                return result;
        }

        return null;
    }

    private static string GetGameDisplayName(string rootFolder, string gameDirectory)
    {
        var relativePath = Path.GetRelativePath(rootFolder, gameDirectory);
        var lastSep = relativePath.LastIndexOf(Path.DirectorySeparatorChar);
        if (lastSep < 0)
            return relativePath;

        var parent = relativePath[..lastSep].Replace(Path.DirectorySeparatorChar, '-');
        var folder = relativePath[(lastSep + 1)..];
        return $"{parent} ({folder})";
    }

    internal void CreateBatchFilesForScummVmGames(string rootFolder, string scummvmExePath, string outputFormat, CancellationToken ct)
    {
        try
        {
            var gameDirectories = GetGameFolderCandidates(rootFolder);
            var filesCreated = 0;

            LogMessage("");
            LogMessage("Scanning for game folders (top-down)...");
            UpdateStatusBarMessage("Creating files...");

            LogMessage($"Found {gameDirectories.Count} game folder(s).");
            LogMessage("Validating with ScummVM detection...");

            for (var i = 0; i < gameDirectories.Count; i++)
            {
                if (ct.IsCancellationRequested)
                {
                    LogMessage("");
                    LogMessage("Process cancelled by user.");
                    UpdateStatusBarMessage("Cancelled.");
                    return;
                }

                var gameDirectory = gameDirectories[i];

                try
                {
                    var displayName = GetGameDisplayName(rootFolder, gameDirectory);

                    UpdateStatusBarMessage($"Validating... ({i + 1}/{gameDirectories.Count})");

                    var gameId = DetectGameId(scummvmExePath, gameDirectory, displayName);

                    if (ct.IsCancellationRequested)
                    {
                        LogMessage("");
                        LogMessage("Process cancelled by user.");
                        UpdateStatusBarMessage("Cancelled.");
                        return;
                    }

                    if (outputFormat == "bat")
                    {
                        var batchFilePath = BatchFileGenerator.WriteBatchFile(rootFolder, gameDirectory, scummvmExePath, displayName);
                        LogMessage($"File created: {batchFilePath}");
                        filesCreated++;

                        if (string.IsNullOrEmpty(gameId))
                            LogMessage("  Note: ScummVM could not auto-detect this game. The batch file will use --auto-detect at launch.");
                    }
                    else if (!string.IsNullOrEmpty(gameId))
                    {
                        var filePath = outputFormat == "simple"
                            ? BatchFileGenerator.WriteSimpleScummVmFile(rootFolder, gameId)
                            : BatchFileGenerator.WriteRocknixScummVmFile(rootFolder, displayName, gameDirectory, gameId);

                        LogMessage($"File created: {filePath}");
                        filesCreated++;
                    }
                    else
                    {
                        LogMessage($"Skipped (game ID required for .scummvm): {displayName}");
                        LogMessage("  Verify the game data is valid in ScummVM's 'Add Game' dialog.");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error creating file for {gameDirectory}: {ex.Message}");
                    _ = ReportBugAsync($"Error creating file for {Path.GetFileName(gameDirectory)}", ex);
                }
            }

            if (filesCreated > 0)
            {
                LogMessage("");
                LogMessage($"{filesCreated} files have been successfully created.");
                LogMessage("They are located in the root folder of your ScummVM games.");
                UpdateStatusBarMessage($"{filesCreated} files created successfully.");

                ShowMessageBox($"{filesCreated} files have been successfully created.\n\n" +
                               "They are located in the root folder of your ScummVM games.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                LogMessage("No ScummVM game folders found. No files were created.");
                ShowError("No ScummVM game folders found. No files were created.");
                UpdateStatusBarMessage("No game folders found. No files were created.");
                _ = ReportBugAsync("No game folders found",
                    new DirectoryNotFoundException("No valid ScummVM game directories were found in the game folder"));
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error accessing folder structure: {ex.Message}");
            UpdateStatusBarMessage("Error accessing folder structure.");
            _ = ReportBugAsync("Error accessing folder structure during file creation", ex);
            throw;
        }
    }

    internal string? DetectGameId(string scummvmExePath, string gameDirectory, string gameFolderName)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = scummvmExePath,
                Arguments = $"-p \"{gameDirectory}\" --detect",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            if (!process.WaitForExit(15000))
            {
                if (!process.HasExited)
                    process.Kill();

                LogMessage($"  Warning: Game ID detection timed out for {gameFolderName}");
            }

            process.WaitForExit();

            var combined = outputBuilder + "\n" + errorBuilder;
            return GameIdDetector.DetectFromOutput(combined);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            LogMessage($"  Warning: Game ID detection error for {gameFolderName}: {ex.Message}");
            return null;
        }
    }

    private void ShowMessageBox(string message, string title, MessageBoxButton buttons, MessageBoxImage icon)
    {
        if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(this, message, title, buttons, icon));
        else
            MessageBox.Show(this, message, title, buttons, icon);
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

            fullReport.Append(App.BuildEnvironmentDetails());

            fullReport.AppendLine("=== Error Details ===");
            fullReport.AppendLine(CultureInfo.InvariantCulture, $"Error Message: {message}");
            fullReport.AppendLine();

            // Add exception details if available
            if (exception != null)
            {
                fullReport.AppendLine("=== Exception Details ===");
                App.AppendExceptionDetails(fullReport, exception);
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
                var version = assemblyName.Version?.ToString();
                var environment = RuntimeInformation.OSDescription;
                var stackTrace = exception?.StackTrace;

                await App.BugReportService.SendBugReportAsync(fullReport.ToString(), version, environment, stackTrace);
            }
        }
        catch
        {
            // Silently fail if error reporting itself fails
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
        CancelButton.IsEnabled = false;
        CancelButton.Content = "Cancelling...";
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
