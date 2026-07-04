using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Scripting;
using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Lighting;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Engine.Scene.Serializer;

internal sealed class ComponentSerializerRegistry : IComponentSerializerRegistry
{
    private const string NameKey = "Name";

    private readonly Dictionary<string, IComponentSerializer> _byName = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, IComponentSerializer> _byType = new();
    private readonly Dictionary<Assembly, List<string>> _assemblyNames = new();

    public ComponentSerializerRegistry()
    {
        RegisterBuiltins();
    }

    public void Register<T>(string? componentName = null) where T : class, IComponent =>
        RegisterSerializer(new JsonComponentSerializer<T>(componentName));

    public void RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        UnregisterAssembly(assembly);

        var names = new List<string>();
        foreach (var type in AssemblyLoadTypes.From(assembly))
        {
            if (type is not { IsClass: true, IsAbstract: false })
                continue;

            if (!typeof(IComponent).IsAssignableFrom(type) || _byType.ContainsKey(type))
                continue;

            if (type.GetCustomAttribute<SerializableComponentAttribute>() is not { } attr)
                continue;

            var serializer = CreateJsonSerializer(type, attr.Name);
            RegisterSerializer(serializer);
            names.Add(serializer.ComponentName);
        }

        if (names.Count > 0)
            _assemblyNames[assembly] = names;
    }

    public void UnregisterAssembly(Assembly assembly)
    {
        if (!_assemblyNames.Remove(assembly, out var names))
            return;

        foreach (var name in names)
        {
            if (!_byName.TryGetValue(name, out var serializer))
                continue;

            if (serializer.ComponentType.Assembly != assembly)
                continue;

            _byName.Remove(name);
            _byType.Remove(serializer.ComponentType);
        }
    }

    public void SerializeEntity(Entity entity, JsonArray componentsArray, JsonSerializerOptions options)
    {
        foreach (var component in entity.GetAllComponents())
        {
            if (!_byType.TryGetValue(component.GetType(), out var serializer))
            {
                throw new InvalidOperationException(
                    $"No serializer registered for component type '{component.GetType().FullName}' on entity '{entity.Name}' (Id={entity.Id}). " +
                    "Refusing to serialize a partial entity — this would silently drop data.");
            }

            if (serializer.TrySerialize(component, options, out var json) && json is not null)
                componentsArray.Add(json);
        }
    }

    public void DeserializeComponent(
        Entity entity,
        JsonObject componentJson,
        JsonSerializerOptions options,
        bool strict)
    {
        if (componentJson[NameKey] is null)
            throw new InvalidSceneJsonException("Invalid component JSON");

        var componentName = componentJson[NameKey]!.GetValue<string>();
        if (!_byName.TryGetValue(componentName, out var serializer))
        {
            if (strict)
                throw new InvalidSceneJsonException($"Unknown component type: {componentName}");
            return;
        }

        serializer.TryDeserialize(entity, componentJson, options);
    }

    private void RegisterSerializer(IComponentSerializer serializer)
    {
        if (_byType.ContainsKey(serializer.ComponentType))
            return;

        if (_byName.TryGetValue(serializer.ComponentName, out var existing))
            _byType.Remove(existing.ComponentType);

        _byName[serializer.ComponentName] = serializer;
        _byType[serializer.ComponentType] = serializer;
    }

    private void RegisterBuiltins()
    {
        Register<TransformComponent>();
        Register<CameraComponent>();
        Register<SpriteRendererComponent>();
        Register<SubTextureRendererComponent>();
        Register<RigidBody2DComponent>();
        Register<BoxCollider2DComponent>();
        Register<AudioListenerComponent>();
        Register<AudioSourceComponent>();
        Register<ModelRendererComponent>();
        Register<AmbientLightComponent>();
        Register<DirectionalLightComponent>();
        RegisterSerializer(new NativeScriptComponentSerializer());
    }

    private static IComponentSerializer CreateJsonSerializer(Type componentType, string? name)
    {
        var serializerType = typeof(JsonComponentSerializer<>).MakeGenericType(componentType);
        return (IComponentSerializer)Activator.CreateInstance(serializerType, name)!;
    }
}
