using Editor.UI.Drawers;

namespace Editor.AssetPicker;

public readonly record struct AssetEntry(string AbsolutePath, string DisplayPath);

public static class AssetScanner
{
    private static readonly HashSet<string> AlwaysSkipDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "Builds", "node_modules", "artifacts", "addons",
    };

    public static List<AssetEntry> Scan(string projectRoot, string[] extensions)
    {
        var results = new List<AssetEntry>();
        if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            return results;

        var root = Path.GetFullPath(projectRoot);
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            if (IsSkippedPath(root, file) || !DragDropDrawer.HasValidExtension(file, extensions))
                continue;

            var display = Path.GetRelativePath(root, file).Replace('\\', '/');
            results.Add(new AssetEntry(file, display));
        }

        results.Sort((a, b) => string.Compare(a.DisplayPath, b.DisplayPath, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    public static bool MatchesSearch(string displayPath, string query, bool fuzzy)
    {
        if (string.IsNullOrWhiteSpace(query))
            return true;

        var fileName = Path.GetFileName(displayPath);
        if (!fuzzy)
            return fileName.Contains(query, StringComparison.OrdinalIgnoreCase);

        // ponytail: subsequence match; upgrade to ranked FZF if search quality becomes an issue
        var qi = 0;
        for (var ti = 0; ti < fileName.Length && qi < query.Length; ti++)
        {
            if (char.ToLowerInvariant(fileName[ti]) == char.ToLowerInvariant(query[qi]))
                qi++;
        }

        return qi == query.Length;
    }

    private static bool IsSkippedPath(string root, string file)
    {
        var relative = Path.GetRelativePath(root, file);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (AlwaysSkipDirs.Contains(segment))
                return true;
        }

        return false;
    }
}
