using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class ApplicationStatsServiceTests
{
    [Fact]
    public void ConstructorStoresConfigurationValues()
    {
        var service = new ApplicationStatsService(
            "https://example.com/stats",
            "test-api-key",
            "app-id-123",
            "1.0.0");

        Assert.NotNull(service);
    }

    [Fact]
    public void ConstructorDoesNotThrowWithValidParameters()
    {
        var exception = Record.Exception(static () =>
            new ApplicationStatsService("https://example.com/stats", "key", "app", "1.0.0"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendUsageStatAsyncDoesNotThrowWhenDisposed()
    {
        var service = new ApplicationStatsService("https://example.com/stats", "key", "app", "1.0.0");
        service.Dispose();

        var exception = await Record.ExceptionAsync(service.SendUsageStatAsync);
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendUsageStatAsyncDoesNotThrowOnInvalidUrl()
    {
        var service = new ApplicationStatsService(
            "https://invalid-nonexistent-url.example/stats", "key", "app", "1.0.0");

        var exception = await Record.ExceptionAsync(service.SendUsageStatAsync);

        Assert.Null(exception);
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        var service = new ApplicationStatsService("https://example.com/stats", "key", "app", "1.0.0");

        var exception = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendUsageStatAsyncAfterDoubleDisposeDoesNotThrow()
    {
        var service = new ApplicationStatsService("https://example.com/stats", "key", "app", "1.0.0");
        service.Dispose();
        service.Dispose();

        var exception = await Record.ExceptionAsync(service.SendUsageStatAsync);

        Assert.Null(exception);
    }
}
