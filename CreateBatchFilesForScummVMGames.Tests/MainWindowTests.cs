using CreateBatchFilesForScummVMGames.Services;
using Xunit;

namespace CreateBatchFilesForScummVMGames.Tests;

public class MainWindowTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true);
        GC.SuppressFinalize(this);
    }

    private string CreateGameDir(string name)
    {
        var dir = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static List<string> SimulateBatchFileCreation(string rootFolder, string scummvmExePath)
    {
        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);

        var gameDirectories = Directory.GetDirectories(rootFolder);
        var createdFiles = new List<string>();

        foreach (var gameDirectory in gameDirectories)
        {
            var path = BatchFileGenerator.WriteBatchFile(rootFolder, gameDirectory, scummvmExePath);
            createdFiles.Add(path);
        }

        return createdFiles;
    }

    private static List<string> SimulateScummVmCreation(string rootFolder, string scummvmExePath)
    {
        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);

        var gameDirectories = Directory.GetDirectories(rootFolder);
        var createdFiles = new List<string>();

        foreach (var gameDirectory in gameDirectories)
        {
            var gameFolderName = Path.GetFileName(gameDirectory);
            var gameId = "detected-" + gameFolderName.ToLowerInvariant().Replace(" ", "-");

            var batchPath = BatchFileGenerator.WriteBatchFile(rootFolder, gameDirectory, scummvmExePath);
            createdFiles.Add(batchPath);

            var (simplePath, rocknixPath) = BatchFileGenerator.WriteScummVmFiles(rootFolder, gameFolderName, gameDirectory, gameId);
            createdFiles.Add(simplePath);
            createdFiles.Add(rocknixPath);
        }

        return createdFiles;
    }

    [Fact]
    public void BatchCreationIntegrationCreatesBatchFileForEachSubdirectory()
    {
        CreateGameDir("MonkeyIsland");
        CreateGameDir("DayOfTheTentacle");

        var files = SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        Assert.Equal(2, files.Count);
        Assert.Contains(Path.Combine(_tempRoot, "MonkeyIsland.bat"), files);
        Assert.Contains(Path.Combine(_tempRoot, "DayOfTheTentacle.bat"), files);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "MonkeyIsland.bat")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "DayOfTheTentacle.bat")));
    }

    [Fact]
    public void BatchCreationIntegrationBatchFileContainsCorrectContent()
    {
        var gameDir = CreateGameDir("MonkeyIsland");

        SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        var batchFile = Path.Combine(_tempRoot, "MonkeyIsland.bat");
        var content = File.ReadAllText(batchFile);
        Assert.Contains($"\"C:\\ScummVM\\scummvm.exe\" -p \"{gameDir}\" --auto-detect --fullscreen", content);
    }

    [Fact]
    public void BatchCreationIntegrationHandlesSpacesInPaths()
    {
        CreateGameDir("Monkey Island CD");

        SimulateBatchFileCreation(_tempRoot, @"C:\Program Files\ScummVM\scummvm.exe");

        var batchFile = Path.Combine(_tempRoot, "Monkey Island CD.bat");
        Assert.True(File.Exists(batchFile));

        var content = File.ReadAllText(batchFile);
        Assert.Contains("\"C:\\Program Files\\ScummVM\\scummvm.exe\"", content);
        Assert.Contains("Monkey Island CD", content);
    }

    [Fact]
    public void BatchCreationIntegrationEmptyRootCreatesNoFiles()
    {
        var files = SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        Assert.Empty(files);
        Assert.Empty(Directory.GetFiles(_tempRoot, "*.bat"));
    }

    [Fact]
    public void BatchCreationIntegrationMultipleDirectoriesAllGetBatchFiles()
    {
        CreateGameDir("GameA");
        CreateGameDir("GameB");
        CreateGameDir("GameC");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Equal(3, files.Count);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "GameA.bat")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "GameB.bat")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "GameC.bat")));
    }

    [Fact]
    public void BatchCreationIntegrationBatchFileNameUsesFolderName()
    {
        CreateGameDir("Full Throttle");

        SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.True(File.Exists(Path.Combine(_tempRoot, "Full Throttle.bat")));
    }

    [Fact]
    public void ScummVmCreationIntegrationCreatesBothSimpleAndRocknixFiles()
    {
        CreateGameDir("Monkey Island");

        var files = SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        Assert.Contains(Path.Combine(_tempRoot, "detected-monkey-island.scummvm"), files);
        Assert.Contains(Path.Combine(_tempRoot, "Monkey Island (detected-monkey-island).scummvm"), files);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "detected-monkey-island.scummvm")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "Monkey Island (detected-monkey-island).scummvm")));
    }

    [Fact]
    public void ScummVmCreationIntegrationSimpleFileContainsGameId()
    {
        CreateGameDir("Monkey Island");

        SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        var content = File.ReadAllText(Path.Combine(_tempRoot, "detected-monkey-island.scummvm"));
        Assert.Equal("detected-monkey-island", content);
    }

    [Fact]
    public void ScummVmCreationIntegrationRocknixFileContainsQuotedPathAndGameId()
    {
        var gameDir = CreateGameDir("Monkey Island");
        const string gameId = "detected-monkey-island";

        SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        var content = File.ReadAllText(Path.Combine(_tempRoot, $"Monkey Island ({gameId}).scummvm"));
        Assert.Contains($"--path=\"{gameDir}\" {gameId}", content);
    }

    [Fact]
    public void ScummVmCreationIntegrationHandlesHyphenatedGameIds()
    {
        CreateGameDir("CoMI");

        SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        Assert.True(File.Exists(Path.Combine(_tempRoot, "detected-comi.scummvm")));
        Assert.True(File.Exists(Path.Combine(_tempRoot, "CoMI (detected-comi).scummvm")));
    }

    [Fact]
    public void BatchCreationIntegrationCorrectContentForEachGame()
    {
        var gameDir1 = CreateGameDir("Monkey Island");
        var gameDir2 = CreateGameDir("Full Throttle");

        SimulateBatchFileCreation(_tempRoot, @"D:\Emulators\ScummVM\scummvm.exe");

        var content1 = File.ReadAllText(Path.Combine(_tempRoot, "Monkey Island.bat"));
        var content2 = File.ReadAllText(Path.Combine(_tempRoot, "Full Throttle.bat"));

        Assert.Contains($"\"D:\\Emulators\\ScummVM\\scummvm.exe\" -p \"{gameDir1}\" --auto-detect --fullscreen", content1);
        Assert.Contains($"\"D:\\Emulators\\ScummVM\\scummvm.exe\" -p \"{gameDir2}\" --auto-detect --fullscreen", content2);
    }

    [Fact]
    public void BatchCreationIntegrationDoesNotCreateScummVmFilesWhenDisabled()
    {
        CreateGameDir("MonkeyIsland");

        SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        var scummvmFiles = Directory.GetFiles(_tempRoot, "*.scummvm");
        Assert.Empty(scummvmFiles);
    }

    [Fact]
    public void ScummVmCreationIntegrationAlsoCreatesBatchFiles()
    {
        CreateGameDir("Monkey Island");

        var files = SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        Assert.Contains(Path.Combine(_tempRoot, "Monkey Island.bat"), files);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "Monkey Island.bat")));
    }

    [Fact]
    public void BatchCreationIntegrationCorrectFileCountMultipleDirectories()
    {
        CreateGameDir("GameA");
        CreateGameDir("GameB");
        CreateGameDir("GameC");
        CreateGameDir("GameD");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Equal(4, files.Count);
    }

    [Fact]
    public void BatchCreationIntegrationGameDirectoriesWithoutFilesAlsoGetBatchFiles()
    {
        CreateGameDir("EmptyGameDir");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "EmptyGameDir.bat")));
    }

    [Fact]
    public void ScummVmCreationIntegrationThreeFilesPerGameDirectory()
    {
        CreateGameDir("Monkey Island");

        var files = SimulateScummVmCreation(_tempRoot, "scummvm.exe");

        Assert.Equal(3, files.Count);
    }
}
