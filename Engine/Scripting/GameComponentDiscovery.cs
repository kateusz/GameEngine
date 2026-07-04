using System.Text.RegularExpressions;

namespace Engine.Scripting;

public static partial class GameComponentDiscovery
{
    [GeneratedRegex(@"public\s+(?:partial\s+)?class\s+(\w+)\b[^{]*\bIGameComponent\b", RegexOptions.Compiled)]
    private static partial Regex ComponentClassRegex();

    public static string[] DiscoverFromScriptsDir(string scriptsDir)
    {
        if (string.IsNullOrWhiteSpace(scriptsDir) || !Directory.Exists(scriptsDir))
            return [];

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var path in GameAssemblyCompiler.EnumerateGameScriptFiles(scriptsDir))
        {
            var content = File.ReadAllText(path);
            foreach (Match match in ComponentClassRegex().Matches(content))
                names.Add(match.Groups[1].Value);
        }

        return names.OrderBy(n => n).ToArray();
    }
}
