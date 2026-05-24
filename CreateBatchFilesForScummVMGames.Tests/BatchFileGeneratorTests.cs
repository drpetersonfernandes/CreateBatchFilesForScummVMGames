using CreateBatchFilesForScummVMGames.Services;
using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class BatchFileGeneratorTests
{
    [Fact]
    public void GenerateBatchFileContentReturnsCorrectFormat()
    {
        var result = BatchFileGenerator.GenerateBatchFileContent(@"C:\ScummVM\scummvm.exe", @"C:\Games\monkey");
        Assert.Equal("""
                     "C:\ScummVM\scummvm.exe" -p "C:\Games\monkey" --auto-detect --fullscreen
                     """, result);
    }

    [Fact]
    public void GenerateBatchFileContentHandlesSpacesInPaths()
    {
        var result = BatchFileGenerator.GenerateBatchFileContent(
            @"C:\Program Files\ScummVM\scummvm.exe",
            @"C:\My Games\Monkey Island");
        Assert.Equal(
            """
            "C:\Program Files\ScummVM\scummvm.exe" -p "C:\My Games\Monkey Island" --auto-detect --fullscreen
            """,
            result);
    }

    [Fact]
    public void GenerateBatchFileNameAppendsBatExtension()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("monkey");
        Assert.Equal("Monkey.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesGameNameWithSpaces()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("Monkey Island CD");
        Assert.Equal("Monkey Island CD.bat", result);
    }

    [Theory]
    [InlineData("Game & Watch", "Game  Watch.bat")]
    [InlineData("Foo ^ Bar", "Foo  Bar.bat")]
    [InlineData("100% Game", "100 Game.bat")]
    [InlineData("Hello! World", "Hello World.bat")]
    [InlineData("A | B", "A  B.bat")]
    [InlineData("Left < Right", "Left  Right.bat")]
    [InlineData("One > Two", "One  Two.bat")]
    [InlineData("A&B^C%D!E|F<G>H", "ABCDEFGH.bat")]
    public void GenerateBatchFileNameStripsBatchProblematicChars(string input, string expected)
    {
        var result = BatchFileGenerator.GenerateBatchFileName(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GenerateBatchFileContentConsistentFormat()
    {
        var content = BatchFileGenerator.GenerateBatchFileContent("scummvm.exe", "game");
        var parts = content.Split(' ');

        Assert.Equal("\"scummvm.exe\"", parts[0]);
        Assert.Equal("-p", parts[1]);
        Assert.Equal("\"game\"", parts[2]);
        Assert.Equal("--auto-detect", parts[3]);
        Assert.Equal("--fullscreen", parts[4]);
    }

    [Fact]
    public void WriteBatchFileCreatesFileWithCorrectContent()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "TestGame");

        try
        {
            Directory.CreateDirectory(gameDir);

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, @"C:\ScummVM\scummvm.exe", "TestGame");

            Assert.True(File.Exists(path));
            Assert.Equal(Path.Combine(tempRoot, "TestGame.bat"), path);
            Assert.Equal(
                "\"C:\\ScummVM\\scummvm.exe\" -p \"" + gameDir + "\" --auto-detect --fullscreen",
                File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteBatchFileHandlesSpacesInPaths()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "My Game Folder");

        try
        {
            Directory.CreateDirectory(gameDir);

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, @"C:\Program Files\ScummVM\scummvm.exe", "My Game Folder");

            Assert.True(File.Exists(path));
            Assert.Equal(Path.Combine(tempRoot, "My Game Folder.bat"), path);
            Assert.Contains("\"C:\\Program Files\\ScummVM\\scummvm.exe\"", File.ReadAllText(path));
            Assert.Contains("My Game Folder", File.ReadAllText(path));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteBatchFileReturnsCorrectFilePath()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "MonkeyIsland");

        try
        {
            Directory.CreateDirectory(gameDir);

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, "scummvm.exe", "MonkeyIsland");

            Assert.Equal(Path.Combine(tempRoot, "MonkeyIsland.bat"), path);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteBatchFileMultipleCallsDontConflict()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir1 = Path.Combine(tempRoot, "GameA");
        var gameDir2 = Path.Combine(tempRoot, "GameB");

        try
        {
            Directory.CreateDirectory(gameDir1);
            Directory.CreateDirectory(gameDir2);

            var path1 = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir1, "scummvm.exe", "GameA");
            var path2 = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir2, "scummvm.exe", "GameB");

            Assert.True(File.Exists(path1));
            Assert.True(File.Exists(path2));
            Assert.NotEqual(path1, path2);
            Assert.Contains("GameA", File.ReadAllText(path1));
            Assert.Contains("GameB", File.ReadAllText(path2));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GenerateBatchFileNameCapitalizesAllWords()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("adventure (monkey island)");
        Assert.Equal("Adventure (Monkey Island).bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesEmptyString()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("");
        Assert.Equal(".bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesOnlySymbols()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("12345");
        Assert.Equal("12345.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesStartingSeparator()
    {
        var result = BatchFileGenerator.GenerateBatchFileName(" the game");
        Assert.Equal(" The Game.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesConsecutiveSeparators()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("foo--bar  baz");
        Assert.Equal("Foo--Bar  Baz.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesParenthesizedPrefix()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("(foo) bar");
        Assert.Equal("(Foo) Bar.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameCapitalizesAfterUnderscore()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("foo_bar baz");
        Assert.Equal("Foo_bar Baz.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesAllProblematicChars()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("&|<>^%!");
        Assert.Equal(".bat", result);
    }
}
