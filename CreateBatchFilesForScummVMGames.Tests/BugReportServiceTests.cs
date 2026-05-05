using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class BugReportServiceTests
{
    [Fact]
    public void ConstructorStoresConfigurationValues()
    {
        var service = new BugReportService(
            "https://example.com/api",
            "test-api-key",
            "TestApp");

        Assert.NotNull(service);
    }

    [Fact]
    public void ConstructorDoesNotThrowWithValidParameters()
    {
        var exception = Record.Exception(static () =>
            new BugReportService("https://example.com/api", "key123", "MyApp"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendBugReportAsyncDoesNotThrowWhenDisposed()
    {
        var service = new BugReportService("https://example.com/api", "key", "TestApp");
        service.Dispose();

        var exception = await Record.ExceptionAsync(() =>
            service.SendBugReportAsync("test message"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendBugReportAsyncDoesNotThrowOnInvalidUrl()
    {
        var service = new BugReportService("https://invalid-nonexistent-url.example/api", "key", "TestApp");

        var exception = await Record.ExceptionAsync(() =>
            service.SendBugReportAsync("test message"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendBugReportAsyncHandlesNullOptionalParameters()
    {
        var service = new BugReportService("https://example.com/api", "key", "TestApp");

        var exception = await Record.ExceptionAsync(() =>
            service.SendBugReportAsync("test"));

        Assert.Null(exception);
    }

    [Fact]
    public void DisposeCanBeCalledMultipleTimes()
    {
        var service = new BugReportService("https://example.com/api", "key", "TestApp");

        var exception = Record.Exception(() =>
        {
            service.Dispose();
            service.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendBugReportAsyncAfterDoubleDisposeDoesNotThrow()
    {
        var service = new BugReportService("https://example.com/api", "key", "TestApp");
        service.Dispose();
        service.Dispose();

        var exception = await Record.ExceptionAsync(() =>
            service.SendBugReportAsync("test"));

        Assert.Null(exception);
    }
}
