using Engine.Project;

namespace Benchmark;

public static class BenchmarkProjectAccess
{
    public static bool TryOpenProject(string projectDir, IProjectContext context, out string? error)
    {
        error = null;
        var full = Path.GetFullPath(Path.IsPathRooted(projectDir)
            ? projectDir
            : Path.Combine(Environment.CurrentDirectory, projectDir));
        if (!Directory.Exists(full))
        {
            error = "Project directory does not exist.";
            return false;
        }

        context.Apply(full);
        return true;
    }

    public static string[] EnumerateScenes(IProjectContext context)
    {
        var root = context.Root;
        var scenesDir = context.ScenesDir;
        if (root is null || scenesDir is null || !Directory.Exists(scenesDir))
            return [];

        return Directory.EnumerateFiles(scenesDir, "*.scene", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool TryApplyProjectForSceneFile(string scenePath, IProjectContext context)
    {
        var full = Path.GetFullPath(scenePath);
        if (context.Root is not null && full.StartsWith(context.Root, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!TryGuessProjectRoot(full, out var root))
            return false;
        context.Apply(root);
        return true;
    }

    public static bool TryGuessProjectRoot(string scenePath, out string projectRoot)
    {
        projectRoot = "";
        var dir = Path.GetDirectoryName(Path.GetFullPath(scenePath));
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "assets")))
            {
                projectRoot = dir;
                return true;
            }

            var parent = Path.GetDirectoryName(dir);
            if (parent == dir)
                break;
            dir = parent;
        }

        return false;
    }
}
