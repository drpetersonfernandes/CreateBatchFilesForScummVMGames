using System.Text.RegularExpressions;

namespace CreateBatchFilesForScummVMGames.Services;

public static partial class GameIdDetector
{
    public static string? DetectFromOutput(string? combinedOutput)
    {
        if (string.IsNullOrEmpty(combinedOutput))
            return null;

        var match = GameIdPattern1().Match(combinedOutput);
        if (match.Success)
            return match.Groups[1].Value;

        match = GameIdPattern2().Match(combinedOutput);
        if (match.Success)
            return match.Groups[1].Value;

        match = GameIdPattern3().Match(combinedOutput);
        if (match.Success)
            return match.Groups[1].Value;

        return null;
    }

    [GeneratedRegex(@"game\s+[Ii][Dd]\s+'([\w-]+)'")]
    internal static partial Regex GameIdPattern1();

    [GeneratedRegex(@"target\s+'([\w-]+)'")]
    internal static partial Regex GameIdPattern2();

    [GeneratedRegex(@"^([a-z][a-z0-9-]+)\s{2,}", RegexOptions.Multiline)]
    internal static partial Regex GameIdPattern3();
}
