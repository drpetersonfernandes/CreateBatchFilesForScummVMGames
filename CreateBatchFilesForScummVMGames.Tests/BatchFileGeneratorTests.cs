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
        Assert.Equal("monkey.bat", result);
    }

    [Fact]
    public void GenerateBatchFileNameHandlesGameNameWithSpaces()
    {
        var result = BatchFileGenerator.GenerateBatchFileName("Monkey Island CD");
        Assert.Equal("Monkey Island CD.bat", result);
    }

    [Fact]
    public void GenerateSimpleScummVmContentReturnsJustGameId()
    {
        var result = BatchFileGenerator.GenerateSimpleScummVmContent("monkey");
        Assert.Equal("monkey", result);
    }

    [Fact]
    public void GenerateSimpleScummVmContentHandlesComplexIds()
    {
        var result = BatchFileGenerator.GenerateSimpleScummVmContent("comi-win");
        Assert.Equal("comi-win", result);
    }

    [Fact]
    public void GenerateSimpleScummVmFileNameCorrectFormat()
    {
        var result = BatchFileGenerator.GenerateSimpleScummVmFileName("monkey");
        Assert.Equal("monkey.scummvm", result);
    }

    [Fact]
    public void GenerateRocknixScummVmContentCorrectFormat()
    {
        var result = BatchFileGenerator.GenerateRocknixScummVmContent(@"C:\Games\monkey", "monkey");
        Assert.Equal("""--path="C:\Games\monkey" monkey""", result);
    }

    [Fact]
    public void GenerateRocknixScummVmContentHandlesSpacesInPaths()
    {
        var result = BatchFileGenerator.GenerateRocknixScummVmContent(
            @"C:\My Games\Monkey Island CD",
            "monkey");
        Assert.Equal("""--path="C:\My Games\Monkey Island CD" monkey""", result);
    }

    [Fact]
    public void GenerateRocknixScummVmFileNameCorrectFormat()
    {
        var result = BatchFileGenerator.GenerateRocknixScummVmFileName("Monkey Island CD", "monkey");
        Assert.Equal("Monkey Island CD (monkey).scummvm", result);
    }

    [Fact]
    public void GenerateRocknixScummVmFileNameHandlesSimpleNames()
    {
        var result = BatchFileGenerator.GenerateRocknixScummVmFileName("monkey", "monkey");
        Assert.Equal("monkey (monkey).scummvm", result);
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

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, @"C:\ScummVM\scummvm.exe");

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

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, @"C:\Program Files\ScummVM\scummvm.exe");

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

            var path = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir, "scummvm.exe");

            Assert.Equal(Path.Combine(tempRoot, "MonkeyIsland.bat"), path);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteScummVmFilesCreatesBothSimpleAndRocknixFiles()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "Monkey Island");

        try
        {
            Directory.CreateDirectory(gameDir);

            var (simplePath, rocknixPath) = BatchFileGenerator.WriteScummVmFiles(tempRoot, "Monkey Island", gameDir, "monkey");

            Assert.True(File.Exists(simplePath));
            Assert.True(File.Exists(rocknixPath));
            Assert.Equal(Path.Combine(tempRoot, "monkey.scummvm"), simplePath);
            Assert.Equal(Path.Combine(tempRoot, "Monkey Island (monkey).scummvm"), rocknixPath);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteScummVmFilesSimpleContentIsGameId()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "Test");

        try
        {
            Directory.CreateDirectory(gameDir);

            var (simplePath, _) = BatchFileGenerator.WriteScummVmFiles(tempRoot, "Test", gameDir, "monkey2");

            Assert.Equal("monkey2", File.ReadAllText(simplePath));
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteScummVmFilesRocknixContentHasQuotedPathAndGameId()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "Test Game");

        try
        {
            Directory.CreateDirectory(gameDir);

            var (_, rocknixPath) = BatchFileGenerator.WriteScummVmFiles(tempRoot, "Test Game", gameDir, "monkey");

            var content = File.ReadAllText(rocknixPath);
            Assert.Contains($"--path=\"{gameDir}\"", content);
            Assert.Contains(" monkey", content);
        }
        finally
        {
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void WriteScummVmFilesHandlesHyphenatedGameIds()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var gameDir = Path.Combine(tempRoot, "CoMI");

        try
        {
            Directory.CreateDirectory(gameDir);

            var (simplePath, rocknixPath) = BatchFileGenerator.WriteScummVmFiles(tempRoot, "CoMI", gameDir, "comi-win");

            Assert.True(File.Exists(simplePath));
            Assert.True(File.Exists(rocknixPath));
            Assert.Equal("comi-win.scummvm", Path.GetFileName(simplePath));
            Assert.Equal("CoMI (comi-win).scummvm", Path.GetFileName(rocknixPath));
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

            var path1 = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir1, "scummvm.exe");
            var path2 = BatchFileGenerator.WriteBatchFile(tempRoot, gameDir2, "scummvm.exe");

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
}
