namespace Engine.Scripting;

public static class GameScriptFiles
{
    public static IEnumerable<string> Enumerate(string scriptsDirectory)
    {
        if (!Directory.Exists(scriptsDirectory))
            return [];

        return Directory
            .EnumerateFiles(scriptsDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(path => ShouldInclude(path, scriptsDirectory));
    }

    private static bool ShouldInclude(string filePath, string scriptsDirectory)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        var fullPath = Path.GetFullPath(filePath);
        var root = Path.GetFullPath(scriptsDirectory);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            return false;

        var relative = Path.GetRelativePath(root, fullPath);
        foreach (var segment in relative.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                segment.Equals(".vs", StringComparison.OrdinalIgnoreCase))
                return false;
        }

        var fileName = Path.GetFileName(fullPath);
        if (fileName.EndsWith(".AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("AssemblyAttributes", StringComparison.OrdinalIgnoreCase))
            return false;

        if (fileName.Equals("GameAssembly.Placeholder.cs", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}
