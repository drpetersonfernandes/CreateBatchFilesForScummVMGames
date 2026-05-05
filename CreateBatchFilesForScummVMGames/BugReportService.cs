using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForScummVMGames;

/// <summary>
/// Service responsible for silently sending bug reports to the BugReport API.
/// This class is designed to be used as a singleton via the App class.
/// </summary>
public class BugReportService : IDisposable
{
    // Use a single, static HttpClient instance for the application's lifetime
    // to prevent socket exhaustion and improve performance.
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private bool _disposed;

    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;

    // Limit to one concurrent HTTP request so that fire-and-forget calls
    // (e.g. per-directory errors in a batch run) do not flood the API.
    private readonly SemaphoreSlim _sendSemaphore = new(1, 1);

    public BugReportService(string apiUrl, string apiKey, string applicationName)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
    }

    /// <summary>
    /// Silently sends a bug report to the API.
    /// </summary>
    /// <param name="message">The error message or bug report.</param>
    /// <param name="version">The application version.</param>
    /// <param name="environment">The runtime environment description.</param>
    /// <param name="stackTrace">The exception stack trace, if available.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendBugReportAsync(string message, string? version = null, string? environment = null, string? stackTrace = null)
    {
        try
        {
            if (_disposed) return;

            // Create the request payload
            var payload = new
            {
                message,
                applicationName = _applicationName,
                version,
                userInfo = (string?)null,
                environment,
                stackTrace
            };

            // Wait for exclusive access so we never flood the API with
            // concurrent requests (important when many failures occur in a
            // batch loop).
            await _sendSemaphore.WaitAsync();

            try
            {
                if (_disposed) return;

                // Create a new HttpRequestMessage for each call. This is thread-safe and ensures
                // headers from one request do not interfere with another.
                using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
                request.Content = JsonContent.Create(payload);
                request.Headers.Add("X-API-KEY", _apiKey);

                // Send the request using the static HttpClient
                await HttpClient.SendAsync(request);
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }
        catch
        {
            // Silently fail if there's an exception
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _sendSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
