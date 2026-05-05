using System.IO;
using System.Text.RegularExpressions;

namespace CreateBatchFilesForScummVMGames.Services;

public static partial class BatchFileGenerator
{
    [GeneratedRegex("[&|<>^%!]")]
    private static partial Regex BatchFileNameInvalidChars();

    public static string GenerateBatchFileContent(string scummvmExePath, string gameDirectory)
    {
        return $"\"{scummvmExePath}\" -p \"{gameDirectory}\" --auto-detect --fullscreen";
    }

    public static string GenerateBatchFileName(string gameFolderName)
    {
        var sanitized = BatchFileNameInvalidChars().Replace(gameFolderName, "");
        return $"{sanitized}.bat";
    }

    public static string GenerateSimpleScummVmContent(string gameId)
    {
        return gameId;
    }

    public static string GenerateSimpleScummVmFileName(string gameId)
    {
        return $"{gameId}.scummvm";
    }

    public static string GenerateRocknixScummVmContent(string gameDirectory, string gameId)
    {
        return $"--path=\"{gameDirectory}\" {gameId}";
    }

    public static string GenerateRocknixScummVmFileName(string gameFolderName, string gameId)
    {
        return $"{gameFolderName} ({gameId}).scummvm";
    }

    internal static string WriteBatchFile(string rootFolder, string gameDirectory, string scummvmExePath)
    {
        var gameFolderName = Path.GetFileName(gameDirectory);
        var batchFilePath = Path.Combine(rootFolder, GenerateBatchFileName(gameFolderName));
        File.WriteAllText(batchFilePath, GenerateBatchFileContent(scummvmExePath, gameDirectory));
        return batchFilePath;
    }

    internal static (string SimplePath, string RocknixPath) WriteScummVmFiles(string rootFolder, string gameFolderName, string gameDirectory, string gameId)
    {
        var simplePath = Path.Combine(rootFolder, GenerateSimpleScummVmFileName(gameId));
        File.WriteAllText(simplePath, GenerateSimpleScummVmContent(gameId));

        var rocknixPath = Path.Combine(rootFolder, GenerateRocknixScummVmFileName(gameFolderName, gameId));
        File.WriteAllText(rocknixPath, GenerateRocknixScummVmContent(gameDirectory, gameId));

        return (simplePath, rocknixPath);
    }
}
