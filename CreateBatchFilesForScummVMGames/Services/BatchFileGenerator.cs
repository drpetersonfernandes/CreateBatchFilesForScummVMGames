using System.IO;
using System.Text.RegularExpressions;

namespace CreateBatchFilesForScummVMGames.Services;

public static partial class BatchFileGenerator
{
    [GeneratedRegex("[&|<>^%!]")]
    private static partial Regex BatchFileNameInvalidChars();

    private static string CapitalizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var chars = name.ToCharArray();
        var capitalize = true;
        for (var i = 0; i < chars.Length; i++)
        {
            if (char.IsLetter(chars[i]))
            {
                if (capitalize)
                {
                    chars[i] = char.ToUpperInvariant(chars[i]);
                    capitalize = false;
                }
            }
            else if (chars[i] is ' ' or '-' or '(')
            {
                capitalize = true;
            }
        }

        return new string(chars);
    }

    public static string GenerateBatchFileContent(string scummvmExePath, string gameDirectory)
    {
        return $"\"{scummvmExePath}\" -p \"{gameDirectory}\" --auto-detect --fullscreen";
    }

    public static string GenerateBatchFileName(string gameFolderName)
    {
        var sanitized = BatchFileNameInvalidChars().Replace(gameFolderName, "");
        return $"{CapitalizeFileName(sanitized)}.bat";
    }

    public static string GenerateSimpleScummVmContent(string gameId)
    {
        return gameId;
    }

    public static string GenerateSimpleScummVmFileName(string gameId)
    {
        return $"{CapitalizeFileName(gameId)}.scummvm";
    }

    public static string GenerateRocknixScummVmContent(string gameDirectory, string gameId)
    {
        return $"--path=\"{gameDirectory}\" {gameId}";
    }

    public static string GenerateRocknixScummVmFileName(string gameFolderName, string gameId)
    {
        return $"{CapitalizeFileName(gameFolderName)} ({CapitalizeFileName(gameId)}).scummvm";
    }

    internal static string WriteBatchFile(string rootFolder, string gameDirectory, string scummvmExePath, string displayName)
    {
        var batchFilePath = Path.Combine(rootFolder, GenerateBatchFileName(displayName));
        File.WriteAllText(batchFilePath, GenerateBatchFileContent(scummvmExePath, gameDirectory));
        return batchFilePath;
    }

    internal static string WriteSimpleScummVmFile(string rootFolder, string gameId)
    {
        var path = Path.Combine(rootFolder, GenerateSimpleScummVmFileName(gameId));
        File.WriteAllText(path, GenerateSimpleScummVmContent(gameId));
        return path;
    }

    internal static string WriteRocknixScummVmFile(string rootFolder, string displayName, string gameDirectory, string gameId)
    {
        var path = Path.Combine(rootFolder, GenerateRocknixScummVmFileName(displayName, gameId));
        File.WriteAllText(path, GenerateRocknixScummVmContent(gameDirectory, gameId));
        return path;
    }

    internal static (string SimplePath, string RocknixPath) WriteScummVmFiles(string rootFolder, string displayName, string gameDirectory, string gameId)
    {
        var simplePath = Path.Combine(rootFolder, GenerateSimpleScummVmFileName(gameId));
        File.WriteAllText(simplePath, GenerateSimpleScummVmContent(gameId));

        var rocknixPath = Path.Combine(rootFolder, GenerateRocknixScummVmFileName(displayName, gameId));
        File.WriteAllText(rocknixPath, GenerateRocknixScummVmContent(gameDirectory, gameId));

        return (simplePath, rocknixPath);
    }
}
