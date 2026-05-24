using System.Net.Http;
using System.Text.Json;

namespace CreateBatchFilesForScummVMGames.Services;

public sealed class ReleaseInfo
{
    public bool IsNewVersionAvailable { get; init; }
    public string? LatestVersion { get; init; }
    public string? ReleaseUrl { get; init; }
}

public sealed class GitHubReleaseService : IDisposable
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private const string GitHubApiUrl =
        "https://api.github.com/repos/drpetersonfernandes/CreateBatchFilesForScummVMGames/releases/latest";

    private bool _disposed;

    public async Task<ReleaseInfo?> CheckForUpdateAsync()
    {
        if (_disposed) return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, GitHubApiUrl);
            request.Headers.Add("User-Agent", "CreateBatchFilesForScummVMGames");
            request.Headers.Add("Accept", "application/vnd.github+json");

            var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var tagName = doc.RootElement.GetProperty("tag_name").GetString();
            var htmlUrl = doc.RootElement.GetProperty("html_url").GetString();

            if (string.IsNullOrEmpty(tagName)) return null;

            var latestVersion = ExtractVersionFromTag(tagName);
            var currentVersion = typeof(App).Assembly.GetName().Version;

            if (currentVersion == null) return null;

            var isNewer = IsVersionNewer(latestVersion, currentVersion);

            return new ReleaseInfo
            {
                IsNewVersionAvailable = isNewer,
                LatestVersion = latestVersion,
                ReleaseUrl = htmlUrl
            };
        }
        catch (Exception ex)
        {
            _ = App.SendBugReportAsync("Failed to check for updates on GitHub", ex);
            return null;
        }
    }

    internal static string ExtractVersionFromTag(string tagName)
    {
        if (tagName.StartsWith("release_", StringComparison.OrdinalIgnoreCase))
            return tagName["release_".Length..];

        return tagName.StartsWith('v') ? tagName[1..] : tagName;
    }

    internal static bool IsVersionNewer(string latestVersion, Version currentVersion)
    {
        if (!Version.TryParse(latestVersion, out var latest)) return false;

        return latest > currentVersion;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
    }
}
