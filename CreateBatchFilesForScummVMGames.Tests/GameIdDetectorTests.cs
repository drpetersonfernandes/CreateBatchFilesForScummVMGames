using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class GameIdDetectorTests
{
    [Fact]
    public void DetectFromOutputNullInputReturnsNull()
    {
        var result = Services.GameIdDetector.DetectFromOutput(null);
        Assert.Null(result);
    }

    [Fact]
    public void DetectFromOutputEmptyStringReturnsNull()
    {
        var result = Services.GameIdDetector.DetectFromOutput("");
        Assert.Null(result);
    }

    [Fact]
    public void DetectFromOutputNoMatchReturnsNull()
    {
        var result = Services.GameIdDetector.DetectFromOutput("some random text without game id");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("game id 'monkey2'")]
    [InlineData("game Id 'monkey2'")]
    [InlineData("game iD 'monkey2'")]
    public void Pattern1MatchesWhenIdKeywordIsMixedCase(string input)
    {
        var fullOutput = $"ScummVM 2.9.0\n{input}\nGame detected!";
        var result = Services.GameIdDetector.DetectFromOutput(fullOutput);
        Assert.Equal("monkey2", result);
    }

    [Fact]
    public void Pattern1MatchesGameIdWithHyphens()
    {
        var result = Services.GameIdDetector.DetectFromOutput("game id 'comi-win'");
        Assert.Equal("comi-win", result);
    }

    [Fact]
    public void Pattern2MatchesTargetFormat()
    {
        var result = Services.GameIdDetector.DetectFromOutput("target 'monkey'");
        Assert.Equal("monkey", result);
    }

    [Fact]
    public void Pattern2MatchesTargetWithHyphens()
    {
        var result = Services.GameIdDetector.DetectFromOutput("target 'comi-win'");
        Assert.Equal("comi-win", result);
    }

    [Theory]
    [InlineData("monkey       ScummVM Game")]
    [InlineData("comi          The Curse of Monkey Island")]
    [InlineData("indy3         Indiana Jones and the Last Crusade")]
    [InlineData("loom-tg16     Loom (TG16)")]
    public void Pattern3MatchesGameIdAtLineStart(string input)
    {
        var result = Services.GameIdDetector.DetectFromOutput(input);
        var expected = input.Split("  ")[0];
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Pattern3RequiresAtLeastTwoSpaces()
    {
        var result = Services.GameIdDetector.DetectFromOutput("monkey ScummVM Game");
        Assert.Null(result);
    }

    [Fact]
    public void DetectFromOutputPattern1HasPriorityWhenAllMatch()
    {
        const string combinedOutput = """

                                      monkey        ScummVM Game
                                      game id 'monkey2'
                                      target 'monkey3'

                                      """;
        var result = Services.GameIdDetector.DetectFromOutput(combinedOutput);
        Assert.Equal("monkey2", result);
    }

    [Fact]
    public void DetectFromOutputPattern2HasPriorityWhenPattern1Missing()
    {
        const string combinedOutput = """

                                      monkey        ScummVM Game
                                      target 'monkey2'

                                      """;
        var result = Services.GameIdDetector.DetectFromOutput(combinedOutput);
        Assert.Equal("monkey2", result);
    }

    [Fact]
    public void DetectFromOutputPattern3UsedAsFallback()
    {
        const string combinedOutput = """

                                      monkey        ScummVM Game
                                      No game id or target found

                                      """;
        var result = Services.GameIdDetector.DetectFromOutput(combinedOutput);
        Assert.Equal("monkey", result);
    }

    [Fact]
    public void Pattern1MatchesRealScummVmOutput()
    {
        const string output = "Using game id 'monkey' for Monkey Island 1";
        var result = Services.GameIdDetector.DetectFromOutput(output);
        Assert.Equal("monkey", result);
    }

    [Fact]
    public void Pattern1DoesNotMatchWithoutQuotes()
    {
        var result = Services.GameIdDetector.DetectFromOutput("game id monkey");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("mi1")]
    [InlineData("monkey-island")]
    [InlineData("ft")]
    [InlineData("comi")]
    [InlineData("dig")]
    [InlineData("atlantis")]
    [InlineData("sword1")]
    [InlineData("sky")]
    [InlineData("queen")]
    public void Pattern1MatchesVariousCommonGameIds(string gameId)
    {
        var result = Services.GameIdDetector.DetectFromOutput($"game id '{gameId}'");
        Assert.Equal(gameId, result);
    }

    [Fact]
    public void Pattern1DoesNotMatchEmptyId()
    {
        var result = Services.GameIdDetector.DetectFromOutput("game id ''");
        Assert.Null(result);
    }
}
