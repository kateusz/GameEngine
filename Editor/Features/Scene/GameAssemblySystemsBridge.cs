using ECS.Systems;

namespace Editor.Features.Scene;

public sealed class GameAssemblySystemsBridge(
    Func<string, bool> ensureRegistered,
    Func<IEnumerable<IGameSystem>> resolveSystems) : IGameAssemblySystemsBridge
{
    public bool EnsureRegistered(string assemblyName)
    {
        return ensureRegistered(assemblyName);
    }

    public IReadOnlyList<IGameSystem> ResolveSystems()
    {
        return resolveSystems().ToList();
    }
}
