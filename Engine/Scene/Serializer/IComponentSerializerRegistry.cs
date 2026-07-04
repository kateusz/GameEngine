using System.Reflection;
using ECS;

namespace Engine.Scene.Serializer;

public interface IComponentSerializerRegistry
{
    void Register<T>(string? componentName = null) where T : class, IComponent;

    void RegisterFromAssembly(Assembly assembly);

    void UnregisterAssembly(Assembly assembly);
}
