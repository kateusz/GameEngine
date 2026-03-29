using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Audio;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scripting;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class ComponentDeserializer(
    IAudioEngine audioEngine,
    ITextureFactory textureFactory,
    IMeshFactory meshFactory,
    IScriptEngine scriptEngine,
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
            case nameof(AnimationComponent):
                AddComponent<AnimationComponent>(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
            case nameof(LightingComponent):
                AddComponent<LightingComponent>(entity, componentObj);
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
            case nameof(AnimationComponent):
                AddComponent<AnimationComponent>(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
            case nameof(LightingComponent):
                AddComponent<LightingComponent>(entity, componentObj);
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

        if (component.ScriptableEntity != null)
            component.ScriptTypeName = component.ScriptableEntity.GetType().Name;

        if (!string.IsNullOrEmpty(component.ScriptTypeName))
            obj[ScriptTypeKey] = component.ScriptTypeName;

        if (component.ScriptableEntity != null)
        {
            var fieldsObj = new JsonObject();
            foreach (var (fieldName, _, fieldValue) in component.ScriptableEntity.GetExposedFields())
                fieldsObj[fieldName] = JsonSerializer.SerializeToNode(fieldValue, _options);

            if (fieldsObj.Count > 0)
                obj["Fields"] = fieldsObj;
        }

        return obj;
    }

    private void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SpriteRendererComponent>(_options);
        if (component == null) return;

        if (!string.IsNullOrWhiteSpace(component.TexturePath))
            component.Texture = textureFactory.Create(PathBuilder.Build(component.TexturePath));

        entity.AddComponent(component);
    }

    private void DeserializeSubTextureRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SubTextureRendererComponent>(_options);
        if (component == null) 
            return;
        
        if (!string.IsNullOrWhiteSpace(component.TexturePath))
            component.Texture = textureFactory.Create(PathBuilder.Build(component.TexturePath));

        entity.AddComponent(component);
    }

    private void DeserializeAudioSourceComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<AudioSourceComponent>(_options);
        if (component == null) return;

        if (!string.IsNullOrWhiteSpace(component.AudioClipPath))
        {
            try
            {
                var fullPath = Path.Combine(PathBuilder.Build(component.AudioClipPath!));
                component.AudioClip = audioEngine.LoadAudioClip(fullPath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to load audio clip '{AudioClipPath}' for entity '{EntityName}'. Audio component will be created without clip.",
                    component.AudioClipPath, entity.Name);
            }
        }

        entity.AddComponent(component);
    }

    private void DeserializeMeshComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<MeshComponent>(_options);
        if (component == null) return;

        if (!string.IsNullOrWhiteSpace(component.ModelPath))
        {
            var fullPath = PathBuilder.Build(component.ModelPath!);
            try
            {
                var result = meshFactory.LoadModel(fullPath);
                if (component.MeshIndex.HasValue && component.MeshIndex.Value < result.Meshes.Count)
                {
                    component.Meshes = [result.Meshes[component.MeshIndex.Value]];
                }
                else
                {
                    component.SetModel(result.Meshes, fullPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to load model '{ModelPath}' for entity '{EntityName}'. Mesh component will be created without meshes.",
                    component.ModelPath, entity.Name);
            }
        }
        else
        {
            try
            {
                component.SetModel([meshFactory.CreateCube()]);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to create cube for entity '{EntityName}'.", entity.Name);
            }
        }

        entity.AddComponent(component);
    }

    private void DeserializeModelRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<ModelRendererComponent>(_options);
        if (component == null) return;

        foreach (var material in component.Materials)
        {
            if (!string.IsNullOrWhiteSpace(material.DiffuseTexturePath))
                material.DiffuseTexture = textureFactory.Create(PathBuilder.Build(material.DiffuseTexturePath));
            if (!string.IsNullOrWhiteSpace(material.SpecularTexturePath))
                material.SpecularTexture = textureFactory.Create(PathBuilder.Build(material.SpecularTexturePath));
            if (!string.IsNullOrWhiteSpace(material.NormalTexturePath))
                material.NormalTexture = textureFactory.Create(PathBuilder.Build(material.NormalTexturePath));
        }

        entity.AddComponent(component);
    }

    private void DeserializeNativeScriptComponent(Entity entity, JsonObject componentObj)
    {
        var component = new NativeScriptComponent();

        var scriptTypeName = componentObj[ScriptTypeKey]?.GetValue<string>();
        if (!string.IsNullOrEmpty(scriptTypeName))
        {
            component.ScriptTypeName = scriptTypeName;

            var result = scriptEngine.CreateScriptInstance(scriptTypeName);
            if (result.IsSuccess)
            {
                var scriptInstance = result.Value;
                if (componentObj["Fields"] is JsonObject fieldsObj)
                {
                    foreach (var field in fieldsObj)
                    {
                        if (field.Value == null) continue;
                        var exposed = scriptInstance.GetExposedFields()
                            .FirstOrDefault(f => f.Name == field.Key);
                        if (exposed.Name != null)
                        {
                            var value = field.Value.Deserialize(exposed.Type, _options);
                            scriptInstance.SetFieldValue(field.Key, value);
                        }
                    }
                }

                component.ScriptableEntity = scriptInstance;
            }
            else
            {
                Logger.Warning(
                    "Failed to instantiate script '{ScriptTypeName}' for entity '{EntityName}'. Component will retain ScriptTypeName for re-serialization.",
                    scriptTypeName, entity.Name);
            }
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