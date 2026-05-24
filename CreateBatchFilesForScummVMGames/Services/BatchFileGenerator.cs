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

    internal static string WriteBatchFile(string rootFolder, string gameDirectory, string scummvmExePath, string displayName)
    {
        var batchFilePath = Path.Combine(rootFolder, GenerateBatchFileName(displayName));
        File.WriteAllText(batchFilePath, GenerateBatchFileContent(scummvmExePath, gameDirectory));
        return batchFilePath;
    }
}
