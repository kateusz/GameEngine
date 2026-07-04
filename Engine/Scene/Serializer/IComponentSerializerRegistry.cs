using System.Reflection;
using ECS;

namespace Engine.Scene.Serializer;

public interface IComponentSerializerRegistry
{
    void Register<T>(string? componentName = null) where T : class, IComponent;

    void RegisterFromAssembly(Assembly assembly);

    void UnregisterAssembly(Assembly assembly);

    /// <summary>
    /// Registers serializers for IGameComponent types discovered in the scripts directory.
    /// Uses an already-loaded game assembly; does not load a second copy.
    /// </summary>
    void RegisterDiscoveredGameComponents(string scriptsDirectory, Assembly? gameAssembly = null);
}
