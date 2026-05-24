using CreateBatchFilesForScummVMGames.Services;
using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class GitHubReleaseServiceTests
{
    [Fact]
    public void ExtractVersionFromTag_StripsReleasePrefix()
    {
        var result = GitHubReleaseService.ExtractVersionFromTag("release_1.2.1");
        Assert.Equal("1.2.1", result);
    }

    [Fact]
    public void ExtractVersionFromTag_StripsReleasePrefixCaseInsensitive()
    {
        var result = GitHubReleaseService.ExtractVersionFromTag("Release_2.0.0");
        Assert.Equal("2.0.0", result);
    }

    [Fact]
    public void ExtractVersionFromTag_RemovesLeadingV()
    {
        var result = GitHubReleaseService.ExtractVersionFromTag("v1.2.3");
        Assert.Equal("1.2.3", result);
    }

    [Fact]
    public void ExtractVersionFromTag_ReturnsSameStringWhenNoPrefix()
    {
        var result = GitHubReleaseService.ExtractVersionFromTag("1.2.3");
        Assert.Equal("1.2.3", result);
    }

    [Fact]
    public void ExtractVersionFromTag_HandlesEmptyString()
    {
        var result = GitHubReleaseService.ExtractVersionFromTag("");
        Assert.Equal("", result);
    }

    [Theory]
    [InlineData("1.2.2", "1.2.1", true)]
    [InlineData("2.0.0", "1.9.9", true)]
    [InlineData("1.3.0", "1.2.5", true)]
    [InlineData("1.2.1", "1.2.1", false)]
    [InlineData("1.2.0", "1.2.1", false)]
    [InlineData("1.1.9", "1.2.0", false)]
    [InlineData("0.9.0", "1.0.0", false)]
    [InlineData("1.2.1.0", "1.2.1", true)]
    public void IsVersionNewer_ReturnsExpectedResult(string latest, string current, bool expected)
    {
        var currentVersion = new Version(current);
        var result = GitHubReleaseService.IsVersionNewer(latest, currentVersion);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsVersionNewer_ReturnsFalseForInvalidVersionString()
    {
        var currentVersion = new Version("1.0.0");
        var result = GitHubReleaseService.IsVersionNewer("not-a-version", currentVersion);
        Assert.False(result);
    }

    [Fact]
    public void IsVersionNewer_ReturnsFalseForEmptyString()
    {
        var currentVersion = new Version("1.0.0");
        var result = GitHubReleaseService.IsVersionNewer("", currentVersion);
        Assert.False(result);
    }

    [Fact]
    public void ConstructorDoesNotThrow()
    {
        var exception = Record.Exception(static () => new GitHubReleaseService());
        Assert.Null(exception);
    }

    [Fact]
    public async Task CheckForUpdateAsyncDoesNotThrowWhenDisposed()
    {
        var service = new GitHubReleaseService();
        service.Dispose();

        var exception = await Record.ExceptionAsync(service.CheckForUpdateAsync);
        Assert.Null(exception);
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        var service = new GitHubReleaseService();

        var exception = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task CheckForUpdateAsyncAfterDoubleDisposeDoesNotThrow()
    {
        var service = new GitHubReleaseService();
        service.Dispose();
        service.Dispose();

        var exception = await Record.ExceptionAsync(service.CheckForUpdateAsync);
        Assert.Null(exception);
    }

    [Fact]
    public void ReleaseInfo_PropertiesAreSetCorrectly()
    {
        var info = new ReleaseInfo
        {
            IsNewVersionAvailable = true,
            LatestVersion = "2.0.0",
            ReleaseUrl = "https://github.com/example/releases/tag/v2.0.0"
        };

        Assert.True(info.IsNewVersionAvailable);
        Assert.Equal("2.0.0", info.LatestVersion);
        Assert.Equal("https://github.com/example/releases/tag/v2.0.0", info.ReleaseUrl);
    }

    [Fact]
    public void ReleaseInfo_DefaultValuesAreNull()
    {
        var info = new ReleaseInfo();

        Assert.False(info.IsNewVersionAvailable);
        Assert.Null(info.LatestVersion);
        Assert.Null(info.ReleaseUrl);
    }
}
