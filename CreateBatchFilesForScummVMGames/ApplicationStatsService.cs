using System.Net.Http;
using System.Net.Http.Json;

namespace CreateBatchFilesForScummVMGames;

public class ApplicationStatsService : IDisposable
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    private bool _disposed;

    private readonly string _statsApiUrl;
    private readonly string _statsApiKey;
    private readonly string _applicationId;
    private readonly string _version;

    public ApplicationStatsService(string statsApiUrl, string statsApiKey, string applicationId, string version)
    {
        _statsApiUrl = statsApiUrl;
        _statsApiKey = statsApiKey;
        _applicationId = applicationId;
        _version = version;
    }

    public async Task SendUsageStatAsync()
    {
        try
        {
            if (_disposed) return;

            var payload = new
            {
                applicationId = _applicationId,
                version = _version
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _statsApiUrl);
            request.Content = JsonContent.Create(payload);
            request.Headers.Add("Authorization", $"Bearer {_statsApiKey}");

            await HttpClient.SendAsync(request);
        }
        catch
        {
            // Silently fail if the stats API is unreachable
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
