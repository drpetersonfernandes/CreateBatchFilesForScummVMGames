using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace CreateBatchFilesForScummVMGames;

/// <inheritdoc cref="System.Windows.Application" />
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // Bug Report API configuration is now centralized here.
    private const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";
    private const string BugReportApiKey = "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";
    private const string ApplicationName = "CreateBatchFilesForScummVMGames";

    // Application Stats API configuration.
    private const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";
    private const string StatsApplicationId = "createbatchfilesforscummvmgames";

    private static bool _isShuttingDown;

    /// <summary>
    /// Provides a single, shared instance of the BugReportService for the entire application.
    /// </summary>
    public static BugReportService? BugReportService { get; private set; }

    /// <summary>
    /// Provides a single, shared instance of the ApplicationStatsService for the entire application.
    /// </summary>
    public static ApplicationStatsService? ApplicationStatsService { get; private set; }

    public App()
    {
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "1.0.0";

        // Initialize the single bug report service instance for the application.
        BugReportService = new BugReportService(BugReportApiUrl, BugReportApiKey, ApplicationName);

        // Initialize the application stats service and track this launch.
        ApplicationStatsService = new ApplicationStatsService(StatsApiUrl, BugReportApiKey, StatsApplicationId, version);
        TrackApplicationLaunch();

        // Set up global exception handling
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private static async void TrackApplicationLaunch()
    {
        try
        {
            if (ApplicationStatsService != null)
            {
                await ApplicationStatsService.SendUsageStatAsync();
            }
        }
        catch
        {
            // Silently ignore any errors in the tracking process
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _isShuttingDown = true;
        try
        {
            BugReportService?.Dispose();
        }
        catch
        {
            // ignored
        }

        try
        {
            ApplicationStatsService?.Dispose();
        }
        catch
        {
            // ignored
        }

        base.OnExit(e);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (_isShuttingDown) return;

        if (e.ExceptionObject is Exception exception)
        {
            ReportExceptionAsync(exception, "AppDomain.UnhandledException");
        }
    }

    private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_isShuttingDown) return;

        ReportExceptionAsync(e.Exception, "Application.DispatcherUnhandledException");
        e.Handled = true;
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        if (_isShuttingDown) return;

        ReportExceptionAsync(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static async void ReportExceptionAsync(Exception exception, string source)
    {
        try
        {
            var message = BuildExceptionReport(exception, source);
            var version = typeof(App).Assembly.GetName().Version?.ToString();
            var environment = RuntimeInformation.OSDescription;
            var stackTrace = exception.StackTrace;

            // Silently report the exception to our API using the shared service instance.
            if (BugReportService != null)
            {
                await BugReportService.SendBugReportAsync(message, version, environment, stackTrace);
            }
        }
        catch
        {
            // Silently ignore any errors in the reporting process
        }
    }

    internal static string BuildEnvironmentDetails()
    {
        var sb = new StringBuilder();
        var assemblyName = typeof(App).Assembly.GetName();

        sb.AppendLine("=== Environment Details ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Name: {assemblyName.Name}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Application Version: {assemblyName.Version}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"OS Version: {Environment.OSVersion}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Architecture: {RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Bitness: {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Windows Version: {RuntimeInformation.OSDescription}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Base Directory: {AppContext.BaseDirectory}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Temp Path: {Path.GetTempPath()}");
        sb.AppendLine();

        return sb.ToString();
    }

    internal static string BuildExceptionReport(Exception exception, string source)
    {
        var sb = new StringBuilder();

        sb.Append(BuildEnvironmentDetails());

        sb.AppendLine("=== Error Details ===");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Error Source: {source}");
        sb.AppendLine(CultureInfo.InvariantCulture, $"Error Message: {exception.Message}");
        sb.AppendLine();

        // Add exception details
        sb.AppendLine("=== Exception Details ===");
        AppendExceptionDetails(sb, exception);

        return sb.ToString();
    }

    internal static void AppendExceptionDetails(StringBuilder sb, Exception exception, int level = 0)
    {
        var ex = exception;
        while (ex != null)
        {
            var indent = new string(' ', level * 2);

            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Type: {ex.GetType().FullName}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Message: {ex.Message}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Source: {ex.Source}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}StackTrace:");
            sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}{ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}Inner Exception:");
            }

            ex = ex.InnerException;
            level += 1;
        }
    }
}
