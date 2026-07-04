using Serilog;

namespace Editor.Publisher;

internal static class RuntimeProjectLocator
{
    private static readonly ILogger Logger = Log.ForContext(typeof(RuntimeProjectLocator));

    public static string? FindRuntimeProjectPath()
    {
        var solutionDir = FindSolutionDirectory();
        if (solutionDir is not null)
        {
            var runtimeCsproj = Path.Combine(solutionDir, "Runtime", "Runtime.csproj");
            if (File.Exists(runtimeCsproj))
            {
                Logger.Debug("Found Runtime project at {Path} (via solution file)", runtimeCsproj);
                return runtimeCsproj;
            }
        }

        var possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Runtime", "Runtime.csproj"),
            Path.Combine(Environment.CurrentDirectory, "Runtime", "Runtime.csproj"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Runtime", "Runtime.csproj"))
        };

        foreach (var path in possiblePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
                continue;

            Logger.Debug("Found Runtime project at {Path} (via relative path)", fullPath);
            return fullPath;
        }

        Logger.Error("Could not find Runtime.csproj in any known location");
        return null;
    }

    private static string? FindSolutionDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName;

            dir = dir.Parent;
        }

        Logger.Warning("Could not find .sln file");
        return null;
    }
}
