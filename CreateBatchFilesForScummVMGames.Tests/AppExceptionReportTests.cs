using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class AppExceptionReportTests
{
    [Fact]
    public void BuildExceptionReportContainsEnvironmentSection()
    {
        var ex = new InvalidOperationException("Test exception");
        var report = App.BuildExceptionReport(ex, "TestSource");

        Assert.NotNull(report);
        Assert.Contains("=== Environment Details ===", report);
        Assert.Contains("=== Error Details ===", report);
        Assert.Contains("=== Exception Details ===", report);
        Assert.Contains("InvalidOperationException", report);
        Assert.Contains("Test exception", report);
        Assert.Contains("TestSource", report);
    }

    [Fact]
    public void BuildExceptionReportContainsInnerException()
    {
        var inner = new ArgumentException("Inner error");
        var outer = new InvalidOperationException("Outer error", inner);
        var report = App.BuildExceptionReport(outer, "Source");

        Assert.NotNull(report);
        Assert.Contains("Inner Exception:", report);
        Assert.Contains("ArgumentException", report);
        Assert.Contains("Inner error", report);
    }

    [Fact]
    public void BuildExceptionReportContainsEnvironmentInfo()
    {
        var ex = new InvalidOperationException("Test");
        var report = App.BuildExceptionReport(ex, "Test");

        Assert.NotNull(report);
        Assert.Contains("OS Version:", report);
        Assert.Contains("Architecture:", report);
        Assert.Contains("Bitness:", report);
        Assert.Contains("Windows Version:", report);
        Assert.Contains("Processor Count:", report);
        Assert.Contains("Base Directory:", report);
        Assert.Contains("Temp Path:", report);
    }
}
