using Engine.Platform;

namespace Engine.Core;

public static class PathBuilder
{
    private static IProjectContext? _context;

    public static void UseProjectContext(IProjectContext context) => _context = context;

    public static string AssetsPath =>
        _context?.AssetsPath
        ?? throw new InvalidOperationException(
            "PathBuilder not initialized. Resolve IProjectContext after EngineIoCContainer.RegisterCore (initializer wires PathBuilder).");

    public static string Build(string path) => Resolve(path);

    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        path = NormalizeSlashes(path);

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        if (path.Length > 7
            && path.StartsWith("assets", StringComparison.OrdinalIgnoreCase)
            && (path[6] == '/' || path[6] == '\\'))
            path = path[7..];

        return Path.GetFullPath(Path.Combine(AssetsPath, path));
    }

    /// <summary>
    /// True when <paramref name="absolutePath"/> resolves under the current <see cref="AssetsPath"/>
    /// (no <c>..</c> escape). Used to confine imported texture loads.
    /// </summary>
    public static bool IsUnderAssets(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
            return false;

        var assets = Path.GetFullPath(AssetsPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var full = Path.GetFullPath(absolutePath);
        return full.StartsWith(assets + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
               || full.Equals(assets, StringComparison.OrdinalIgnoreCase);
    }

    public static string ToAssetRelativePath(string path)
    {
        var resolved = Resolve(path);
        var relative = Path.GetRelativePath(Path.GetFullPath(AssetsPath), resolved);
        if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
            return relative.Replace('\\', '/');

        return path.Replace('\\', '/');
    }

    private static string NormalizeSlashes(string path)
    {
        if (OSInfo.IsWindows)
            return path.Replace('/', '\\');

        if (OSInfo.IsMacOS)
            return path.Replace('\\', '/');

        return path;
    }
}
