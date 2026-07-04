namespace Engine.Core;

public interface IProjectContext
{
    string? Root { get; }
    string AssetsPath { get; }
    string? ScriptsDir { get; }
    string? ScenesDir { get; }
    bool HasProject { get; }
    void Apply(string projectRoot);
    void Clear();
}
