using ECS.Systems;

namespace Editor.Features.Scene;

public interface IGameAssemblySystemsBridge
{
    bool EnsureRegistered(string assemblyName);
    IReadOnlyList<IGameSystem> ResolveSystems();
}
