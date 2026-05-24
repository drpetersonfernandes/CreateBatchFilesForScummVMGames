using System.Globalization;
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

    [Fact]
    public void BuildEnvironmentDetailsContainsAllExpectedSections()
    {
        var details = App.BuildEnvironmentDetails();

        Assert.NotNull(details);
        Assert.Contains("=== Environment Details ===", details);
        Assert.Contains("Date:", details);
        Assert.Contains("Application Name:", details);
        Assert.Contains("Application Version:", details);
        Assert.Contains("OS Version:", details);
        Assert.Contains("Architecture:", details);
        Assert.Contains("Bitness:", details);
        Assert.Contains("Windows Version:", details);
        Assert.Contains("Processor Count:", details);
        Assert.Contains("Base Directory:", details);
        Assert.Contains("Temp Path:", details);
        Assert.Contains(typeof(App).Assembly.GetName().Name ?? "", details);
    }

    [Fact]
    public void BuildEnvironmentDetailsDateFormatIsCorrect()
    {
        var details = App.BuildEnvironmentDetails();

        var dateLine = details.Split(Environment.NewLine)
            .First(static line => line.StartsWith("Date:", StringComparison.Ordinal));

        var dateString = dateLine["Date: ".Length..];
        var parsed = DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

        Assert.True(parsed, $"Date format should be yyyy-MM-dd HH:mm:ss, but got '{dateString}'");
    }

    [Fact]
    public void AppendExceptionDetailsHandlesDeepNesting()
    {
        var sb = new System.Text.StringBuilder();
        // ReSharper disable once NotResolvedInText
        var inner2 = new ArgumentNullException("param3");
        var inner1 = new InvalidOperationException("Second level", inner2);
        var outer = new InvalidOperationException("First level", inner1);

        App.AppendExceptionDetails(sb, outer);
        var result = sb.ToString();

        Assert.Contains("InvalidOperationException", result);
        Assert.Contains("First level", result);
        Assert.Contains("InvalidOperationException", result);
        Assert.Contains("Second level", result);
        Assert.Contains("ArgumentNullException", result);
        Assert.Contains("param3", result);
        Assert.Contains("Inner Exception:", result);
    }

    [Fact]
    public void AppendExceptionDetailsIncreasesIndentationForNestedLevels()
    {
        var sb = new System.Text.StringBuilder();
        var inner = new ArgumentException("Inner");
        var outer = new InvalidOperationException("Outer", inner);

        App.AppendExceptionDetails(sb, outer);
        var result = sb.ToString();

        Assert.Contains("Type:", result);
        Assert.Contains("  Type:", result);
    }

    [Fact]
    public void AppendExceptionDetailsHandlesExceptionWithoutInner()
    {
        var sb = new System.Text.StringBuilder();
        var ex = new InvalidOperationException("Solo exception");

        App.AppendExceptionDetails(sb, ex);
        var result = sb.ToString();

        Assert.Contains("InvalidOperationException", result);
        Assert.Contains("Solo exception", result);
        Assert.DoesNotContain("Inner Exception:", result);
    }
}
