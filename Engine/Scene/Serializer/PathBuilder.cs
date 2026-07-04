using Engine.Core;
using Engine.Platform;

namespace Engine.Scene.Serializer;

public static class PathBuilder
{
    private static IProjectContext _context = new ProjectContext();

    public static void UseProjectContext(IProjectContext context) => _context = context;

    public static string AssetsPath => _context.AssetsPath;

    public static string Build(string path) => Resolve(path);

    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        path = NormalizeSlashes(path);

        if (Path.IsPathRooted(path))
            return Path.GetFullPath(path);

        return Path.GetFullPath(Path.Combine(_context.AssetsPath, path));
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