using ECS.Systems;
using System.Collections.Generic;

namespace Editor.Features.Scene;

public interface IGameAssemblySystemsBridge
{
    bool EnsureRegistered(string assemblyName);
    IReadOnlyList<IGameSystem> ResolveSystems();
}
