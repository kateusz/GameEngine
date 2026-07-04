namespace Engine.Core;

public sealed class ProjectContext : IProjectContext
{
    private static readonly string DefaultAssetsPath =
        Path.Combine(AppContext.BaseDirectory, "assets");

    public string? Root { get; private set; }
    public string AssetsPath { get; private set; } = DefaultAssetsPath;
    public string? ScriptsDir { get; private set; }
    public string? ScenesDir { get; private set; }
    public bool HasProject => Root != null;

    public void Apply(string projectRoot)
    {
        Root = Path.GetFullPath(projectRoot);
        AssetsPath = Directory.Exists(Path.Combine(Root, "assets"))
            ? Path.Combine(Root, "assets")
            : Root;
        ScriptsDir = Path.Combine(Root, "assets", "scripts");
        ScenesDir = Path.Combine(Root, "assets", "scenes");
    }

    public void Clear()
    {
        Root = null;
        AssetsPath = DefaultAssetsPath;
        ScriptsDir = null;
        ScenesDir = null;
    }
}
