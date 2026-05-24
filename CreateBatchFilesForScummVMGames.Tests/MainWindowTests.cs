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
        File.WriteAllText(Path.Combine(dir, ".gamefile"), string.Empty);
        return dir;
    }

    private void CreateNestedGameDir(string parentName, string gameName)
    {
        var parentDir = Path.Combine(_tempRoot, parentName);
        var gameDir = Path.Combine(parentDir, gameName);
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, ".gamefile"), string.Empty);
    }

    private void CreateEmptyLeafDir(string parentName, string emptyName)
    {
        var parentDir = string.IsNullOrEmpty(parentName) ? _tempRoot : Path.Combine(_tempRoot, parentName);
        var emptyDir = Path.Combine(parentDir, emptyName);
        Directory.CreateDirectory(emptyDir);
    }

    private static List<string> SimulateBatchFileCreation(string rootFolder, string scummvmExePath)
    {
        if (!Directory.Exists(rootFolder))
            Directory.CreateDirectory(rootFolder);

        var gameDirectories = MainWindow.GetGameFolderCandidates(rootFolder);
        var createdFiles = new List<string>();

        foreach (var gameDirectory in gameDirectories)
        {
            var displayName = MainWindow.GetGameDisplayName(rootFolder, gameDirectory);
            var path = BatchFileGenerator.WriteBatchFile(rootFolder, gameDirectory, scummvmExePath, displayName);
            createdFiles.Add(path);
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
    public void BatchCreationIntegrationOnlyCreatesBatFiles()
    {
        CreateGameDir("MonkeyIsland");

        SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        var scummvmFiles = Directory.GetFiles(_tempRoot, "*.scummvm");
        Assert.Empty(scummvmFiles);
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
    public void NestedGameFoldersAreDiscovered()
    {
        CreateNestedGameDir("adventure", "MonkeyIsland");
        CreateNestedGameDir("rpg", "DayOfTheTentacle");

        var files = SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        Assert.Equal(2, files.Count);
        Assert.Contains(Path.Combine(_tempRoot, "Adventure (MonkeyIsland).bat"), files);
        Assert.Contains(Path.Combine(_tempRoot, "Rpg (DayOfTheTentacle).bat"), files);
    }

    [Fact]
    public void IntermediateContainersAreNotTreatedAsGames()
    {
        var container = Path.Combine(_tempRoot, "adventure");
        Directory.CreateDirectory(container);
        CreateNestedGameDir("adventure", "MonkeyIsland");

        var files = SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        Assert.Single(files);
        Assert.Contains("MonkeyIsland", files[0]);
        Assert.DoesNotContain(files, static f => f.EndsWith("adventure.bat", StringComparison.Ordinal));
    }

    [Fact]
    public void EmptyLeafFoldersAreSkipped()
    {
        CreateEmptyLeafDir("", "empty");
        CreateGameDir("MonkeyIsland");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.Contains("MonkeyIsland", files[0]);
        Assert.DoesNotContain(files, static f => f.Contains("empty"));
    }

    [Fact]
    public void EmptyNestedLeafFoldersAreSkipped()
    {
        CreateEmptyLeafDir("adventure", "empty");
        CreateNestedGameDir("adventure", "MonkeyIsland");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.Contains("MonkeyIsland", files[0]);
    }

    [Fact]
    public void NestedNamingUsesParentInParens()
    {
        CreateNestedGameDir("adventure", "Monkey Island");

        var files = SimulateBatchFileCreation(_tempRoot, @"C:\ScummVM\scummvm.exe");

        Assert.Single(files);
        Assert.Contains(Path.Combine(_tempRoot, "Adventure (Monkey Island).bat"), files);
        Assert.True(File.Exists(Path.Combine(_tempRoot, "Adventure (Monkey Island).bat")));
    }

    [Fact]
    public void CollisionAvoidanceForSameLeafName()
    {
        CreateNestedGameDir("scifi", "monkey");
        CreateNestedGameDir("comedy", "monkey");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Equal(2, files.Count);
        Assert.Contains(Path.Combine(_tempRoot, "Scifi (Monkey).bat"), files);
        Assert.Contains(Path.Combine(_tempRoot, "Comedy (Monkey).bat"), files);
    }

    [Fact]
    public void FlatGamesStillUsePlainFolderName()
    {
        CreateGameDir("MonkeyIsland");
        CreateGameDir("FullThrottle");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Equal(2, files.Count);
        Assert.Contains(Path.Combine(_tempRoot, "MonkeyIsland.bat"), files);
        Assert.Contains(Path.Combine(_tempRoot, "FullThrottle.bat"), files);
    }

    [Fact]
    public void DeeplyNestedGameFoldersAreDiscovered()
    {
        var rpgDir = Path.Combine(_tempRoot, "rpg");
        var indyDir = Path.Combine(rpgDir, "lucasarts");
        Directory.CreateDirectory(indyDir);
        var gameDir = Path.Combine(indyDir, "Indy3");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, ".gamefile"), string.Empty);

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.Contains(Path.Combine(_tempRoot, "Rpg-Lucasarts (Indy3).bat"), files);
    }

    [Fact]
    public void OneGamePerFirstLevelFolder()
    {
        CreateNestedGameDir("zork", "data");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.Contains(files, static f => f.EndsWith("Zork (Data).bat", StringComparison.Ordinal));
    }

    [Fact]
    public void StopsAtFirstFolderWithFilesGoingDeep()
    {
        var zork = Path.Combine(_tempRoot, "Zork Nemesis (DVD DOS)");
        var level2 = Path.Combine(zork, "Zork Nemesis (DVD DOS)");
        var level3 = Path.Combine(level2, "Zork Nemesis (DVD DOS)");
        Directory.CreateDirectory(level3);
        File.WriteAllText(Path.Combine(level3, "game.dat"), string.Empty);

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        const string expectedSuffix = "Zork Nemesis (DVD DOS)-Zork Nemesis (DVD DOS) (Zork Nemesis (DVD DOS)).bat";
        Assert.Contains(files, static f => f.EndsWith(expectedSuffix, StringComparison.Ordinal));
    }

    [Fact]
    public void BatchCreationCapitalizesAllWordsInFileName()
    {
        CreateGameDir("the secret of monkey island");

        var files = SimulateBatchFileCreation(_tempRoot, "scummvm.exe");

        Assert.Single(files);
        Assert.Contains(Path.Combine(_tempRoot, "The Secret Of Monkey Island.bat"), files);
    }
}
