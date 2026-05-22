namespace Editor.Features.Project;

public static class GameEngineCheckoutLocator
{
    private const string SolutionFileName = "GameEngine.sln";

    public static string? TryFindEngineCheckoutRoot()
    {
        var start = new DirectoryInfo(AppContext.BaseDirectory);
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                return dir.FullName;
            var props = Path.Combine(dir.FullName, "build", "GameScriptReferences.props");
            if (File.Exists(props))
                return dir.FullName;
        }

        return null;
    }

    public static string? TryGetGameScriptSdkStagingDirectory(string configuration)
    {
        var root = TryFindEngineCheckoutRoot();
        if (root is null)
            return null;
        var staging = Path.Combine(root, "artifacts", "GameScriptSdk", configuration, "net10.0");
        return Directory.Exists(staging) ? staging : null;
    }

#if DEBUG
    public const string DefaultSdkConfiguration = "Debug";
#else
    public const string DefaultSdkConfiguration = "Release";
#endif
}
