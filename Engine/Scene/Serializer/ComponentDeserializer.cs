using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using SceneComponents;
using SceneComponents.Audio;
using SceneComponents.Camera;
using SceneComponents.Lights;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class ComponentDeserializer(
    SerializerOptions serializerOptions)
{
    private static readonly ILogger Logger = Log.ForContext<ComponentDeserializer>();

    private const string NameKey = "Name";
    private const string ScriptTypeKey = "ScriptType";

    private readonly JsonSerializerOptions _options = serializerOptions.Options;

    /// <summary>
    /// Strict mode: throws on unknown component types (used by SceneSerializer).
    /// </summary>
    public void DeserializeComponent(Entity entity, JsonNode componentNode)
    {
        if (componentNode is not JsonObject componentObj || componentObj[NameKey] is null)
            throw new InvalidSceneJsonException("Invalid component JSON");

        var componentName = componentObj[NameKey]!.GetValue<string>();

        switch (componentName)
        {
            case nameof(TransformComponent):
                AddComponent<TransformComponent>(entity, componentObj);
                break;
            case nameof(CameraComponent):
                AddComponent<CameraComponent>(entity, componentObj);
                break;
            case nameof(SpriteRendererComponent):
                DeserializeSpriteRendererComponent(entity, componentObj);
                break;
            case nameof(SubTextureRendererComponent):
                DeserializeSubTextureRendererComponent(entity, componentObj);
                break;
            case nameof(RigidBody2DComponent):
                AddComponent<RigidBody2DComponent>(entity, componentObj);
                break;
            case nameof(BoxCollider2DComponent):
                AddComponent<BoxCollider2DComponent>(entity, componentObj);
                break;
            case nameof(AudioListenerComponent):
                AddComponent<AudioListenerComponent>(entity, componentObj);
                break;
            case nameof(AudioSourceComponent):
                DeserializeAudioSourceComponent(entity, componentObj);
                break;
            case nameof(MeshComponent):
                DeserializeMeshComponent(entity, componentObj);
                break;
            case nameof(ModelRendererComponent):
                DeserializeModelRendererComponent(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
            case nameof(PointLightComponent):
                AddComponent<PointLightComponent>(entity, componentObj);
                break;
            case nameof(DirectionalLightComponent):
                AddComponent<DirectionalLightComponent>(entity, componentObj);
                break;
            case nameof(AmbientLightComponent):
                AddComponent<AmbientLightComponent>(entity, componentObj);
                break;
            default:
                throw new InvalidSceneJsonException($"Unknown component type: {componentName}");
        }
    }

    /// <summary>
    /// Lenient mode: silently skips unknown component types (used by PrefabSerializer for version tolerance).
    /// </summary>
    public void DeserializeComponentLenient(Entity entity, JsonNode componentNode)
    {
        if (componentNode is not JsonObject componentObj || componentObj[NameKey] is null)
            throw new InvalidSceneJsonException("Invalid component JSON in prefab");

        var componentName = componentObj[NameKey]!.GetValue<string>();

        switch (componentName)
        {
            case nameof(TransformComponent):
                AddComponent<TransformComponent>(entity, componentObj);
                break;
            case nameof(CameraComponent):
                AddComponent<CameraComponent>(entity, componentObj);
                break;
            case nameof(SpriteRendererComponent):
                DeserializeSpriteRendererComponent(entity, componentObj);
                break;
            case nameof(SubTextureRendererComponent):
                DeserializeSubTextureRendererComponent(entity, componentObj);
                break;
            case nameof(RigidBody2DComponent):
                AddComponent<RigidBody2DComponent>(entity, componentObj);
                break;
            case nameof(BoxCollider2DComponent):
                AddComponent<BoxCollider2DComponent>(entity, componentObj);
                break;
            case nameof(AudioListenerComponent):
                AddComponent<AudioListenerComponent>(entity, componentObj);
                break;
            case nameof(AudioSourceComponent):
                DeserializeAudioSourceComponent(entity, componentObj);
                break;
            case nameof(MeshComponent):
                DeserializeMeshComponent(entity, componentObj);
                break;
            case nameof(ModelRendererComponent):
                DeserializeModelRendererComponent(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
            case nameof(PointLightComponent):
                AddComponent<PointLightComponent>(entity, componentObj);
                break;
            case nameof(DirectionalLightComponent):
                AddComponent<DirectionalLightComponent>(entity, componentObj);
                break;
            case nameof(AmbientLightComponent):
                AddComponent<AmbientLightComponent>(entity, componentObj);
                break;
            // Unknown types silently skipped (version tolerance)
        }
    }

    /// <summary>Serialize NativeScriptComponent into a JsonObject that has a ComponentsKey child array.</summary>
    public void SerializeNativeScriptComponent(Entity entity, JsonObject targetObj, string componentsKey)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        var scriptComponentObj = BuildNativeScriptJson(component);

        var components = targetObj[componentsKey] as JsonArray
                         ?? throw new InvalidSceneJsonException($"'{componentsKey}' must be a JSON array");
        components.Add(scriptComponentObj);
    }

    /// <summary>Serialize NativeScriptComponent directly into a JsonArray.</summary>
    public void SerializeNativeScriptComponentToArray(Entity entity, JsonArray componentsArray)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        componentsArray.Add(BuildNativeScriptJson(component));
    }

    private JsonObject BuildNativeScriptJson(NativeScriptComponent component)
    {
        var obj = new JsonObject { [NameKey] = nameof(NativeScriptComponent) };

        if (!string.IsNullOrEmpty(component.ScriptTypeName))
            obj[ScriptTypeKey] = component.ScriptTypeName;

        return obj;
    }

    private void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SpriteRendererComponent>(_options);
        if (component == null) return;

        entity.AddComponent(component);
    }

    private void DeserializeSubTextureRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SubTextureRendererComponent>(_options);
        if (component == null) 
            return;

        entity.AddComponent(component);
    }

    private void DeserializeAudioSourceComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<AudioSourceComponent>(_options);
        if (component == null) return;

        entity.AddComponent(component);
    }

    private void DeserializeMeshComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<MeshComponent>(_options);
        if (component == null) return;

        entity.AddComponent(component);
    }

    private void DeserializeModelRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<ModelRendererComponent>(_options);
        if (component == null) return;

        entity.AddComponent(component);
    }

    private void DeserializeNativeScriptComponent(Entity entity, JsonObject componentObj)
    {
        var component = new NativeScriptComponent();

        var scriptTypeName = componentObj[ScriptTypeKey]?.GetValue<string>();
        if (!string.IsNullOrEmpty(scriptTypeName))
        {
            component.ScriptTypeName = scriptTypeName;
        }

        entity.AddComponent(component);
    }

    private void AddComponent<T>(Entity entity, JsonObject componentObj) where T : class, IComponent
    {
        var component = componentObj.Deserialize<T>(_options);
        if (component != null)
            entity.AddComponent(component);
    }
}