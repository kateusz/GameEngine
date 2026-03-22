using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ECS;
using Engine.Audio;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class PrefabSerializer(
    IAudioEngine audioEngine,
    ITextureFactory textureFactory,
    IMeshFactory meshFactory,
    SerializerOptions serializerOptions) : IPrefabSerializer
{
    private static readonly ILogger Logger = Log.ForContext<PrefabSerializer>();

    private const string PrefabKey = "Prefab";
    private const string PrefabVersion = "1.0";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string IdKey = "Id";
    private const string VersionKey = "Version";
    private const string ScriptTypeKey = "ScriptType";
    private const string PrefabAssetsDirectory = "assets/prefabs";

    private readonly JsonSerializerOptions _defaultSerializerOptions = serializerOptions.Options;

    /// <summary>
    /// Serialize an entity to a prefab file
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="prefabName">Name of the prefab file (without extension)</param>
    /// <param name="projectPath">Path to the project root</param>
    public void SerializeToPrefab(Entity entity, string prefabName, string projectPath)
    {
        var prefabDir = Path.Combine(projectPath, PrefabAssetsDirectory);
        Directory.CreateDirectory(prefabDir);

        var prefabPath = Path.Combine(prefabDir, $"{prefabName}.prefab");

        var jsonObj = new JsonObject
        {
            [PrefabKey] = prefabName,
            [VersionKey] = PrefabVersion,
            ["OriginalName"] = entity.Name,
            [ComponentsKey] = new JsonArray()
        };

        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        SerializeEntityComponents(entity, componentsArray);

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(prefabPath, jsonString);
    }

    /// <summary>
    /// Apply prefab data to an existing entity (replaces all components)
    /// </summary>
    /// <param name="entity">The entity to apply prefab to</param>
    /// <param name="prefabPath">Path to the prefab file</param>
    public void ApplyPrefabToEntity(Entity entity, string prefabPath)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var json = File.ReadAllText(prefabPath);
        var jsonObj = JsonNode.Parse(json)?.AsObject() ??
                      throw new InvalidSceneJsonException($"Invalid prefab JSON in {prefabPath}");

        // Clear existing components (except ID and Name)
        ClearEntityComponents(entity);

        // Apply prefab components
        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity, componentNode ??
                                         throw new InvalidSceneJsonException("Got null JSON Component in prefab"));
        }
    }

    /// <summary>
    /// Create a new entity from a prefab
    /// </summary>
    /// <param name="prefabPath">Path to the prefab file</param>
    /// <param name="entityName">Name for the new entity</param>
    /// <param name="entityId">ID for the new entity</param>
    /// <returns>New entity with prefab components</returns>
    public Entity CreateEntityFromPrefab(string prefabPath, string entityName, int entityId)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var json = File.ReadAllText(prefabPath);
        var jsonObj = JsonNode.Parse(json)?.AsObject() ??
                      throw new InvalidSceneJsonException($"Invalid prefab JSON in {prefabPath}");

        var entity = Entity.Create(entityId, entityName);

        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity, componentNode ??
                                         throw new InvalidSceneJsonException("Got null JSON Component in prefab"));
        }

        return entity;
    }

    private void SerializeEntityComponents(Entity entity, JsonArray componentsArray)
    {
        SerializeComponent<TransformComponent>(entity, componentsArray, nameof(TransformComponent));
        SerializeComponent<CameraComponent>(entity, componentsArray, nameof(CameraComponent));
        SerializeComponent<SpriteRendererComponent>(entity, componentsArray, nameof(SpriteRendererComponent));
        SerializeComponent<SubTextureRendererComponent>(entity, componentsArray, nameof(SubTextureRendererComponent));
        SerializeComponent<RigidBody2DComponent>(entity, componentsArray, nameof(RigidBody2DComponent));
        SerializeComponent<BoxCollider2DComponent>(entity, componentsArray, nameof(BoxCollider2DComponent));
        SerializeComponent<MeshComponent>(entity, componentsArray, nameof(MeshComponent));
        SerializeComponent<ModelRendererComponent>(entity, componentsArray, nameof(ModelRendererComponent));
        SerializeComponent<AudioListenerComponent>(entity, componentsArray, nameof(AudioListenerComponent));
        SerializeComponent<AnimationComponent>(entity, componentsArray, nameof(AnimationComponent));
        SerializeAudioSourceComponent(entity, componentsArray);
        SerializeNativeScriptComponent(entity, componentsArray);
    }

    private void SerializeComponent<T>(Entity entity, JsonArray componentsArray, string componentName)
        where T : IComponent
    {
        if (!entity.HasComponent<T>())
            return;

        var component = entity.GetComponent<T>();
        var element = JsonSerializer.SerializeToNode(component, _defaultSerializerOptions);
        if (element != null)
        {
            element[NameKey] = componentName;
            componentsArray.Add(element);
        }
    }

    private void SerializeAudioSourceComponent(Entity entity, JsonArray componentsArray)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
            return;

        var component = entity.GetComponent<AudioSourceComponent>();
        var element = JsonSerializer.SerializeToNode(component, _defaultSerializerOptions);
        if (element != null)
        {
            element[NameKey] = nameof(AudioSourceComponent);
            componentsArray.Add(element);
        }
    }

    private void SerializeNativeScriptComponent(Entity entity, JsonArray componentsArray)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        var scriptComponentObj = new JsonObject
        {
            [NameKey] = nameof(NativeScriptComponent)
        };

        // Sync ScriptTypeName from runtime ScriptableEntity if available
        if (component.ScriptableEntity != null)
            component.ScriptTypeName = component.ScriptableEntity.GetType().Name;

        if (!string.IsNullOrEmpty(component.ScriptTypeName))
        {
            scriptComponentObj[ScriptTypeKey] = component.ScriptTypeName;
        }

        // Serialize exposed fields if a script instance is attached
        if (component.ScriptableEntity != null)
        {
            var fieldsObj = new JsonObject();
            foreach (var (fieldName, fieldType, fieldValue) in component.ScriptableEntity.GetExposedFields())
            {
                fieldsObj[fieldName] = JsonSerializer.SerializeToNode(fieldValue, _defaultSerializerOptions);
            }

            if (fieldsObj.Count > 0)
                scriptComponentObj["Fields"] = fieldsObj;
        }

        componentsArray.Add(scriptComponentObj);
    }

    private void ClearEntityComponents(Entity entity)
    {
        if (entity.HasComponent<TransformComponent>())
            entity.RemoveComponent<TransformComponent>();
        if (entity.HasComponent<CameraComponent>())
            entity.RemoveComponent<CameraComponent>();
        if (entity.HasComponent<SpriteRendererComponent>())
            entity.RemoveComponent<SpriteRendererComponent>();
        if (entity.HasComponent<SubTextureRendererComponent>())
            entity.RemoveComponent<SubTextureRendererComponent>();
        if (entity.HasComponent<RigidBody2DComponent>())
            entity.RemoveComponent<RigidBody2DComponent>();
        if (entity.HasComponent<BoxCollider2DComponent>())
            entity.RemoveComponent<BoxCollider2DComponent>();
        if (entity.HasComponent<MeshComponent>())
            entity.RemoveComponent<MeshComponent>();
        if (entity.HasComponent<ModelRendererComponent>())
            entity.RemoveComponent<ModelRendererComponent>();
        if (entity.HasComponent<AudioListenerComponent>())
            entity.RemoveComponent<AudioListenerComponent>();
        if (entity.HasComponent<AudioSourceComponent>())
            entity.RemoveComponent<AudioSourceComponent>();
        if (entity.HasComponent<AnimationComponent>())
            entity.RemoveComponent<AnimationComponent>();
        if (entity.HasComponent<NativeScriptComponent>())
            entity.RemoveComponent<NativeScriptComponent>();
    }

    private void DeserializeComponent(Entity entity, JsonNode componentNode)
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
            case nameof(MeshComponent):
                DeserializeMeshComponent(entity, componentObj);
                break;
            case nameof(ModelRendererComponent):
                DeserializeModelRendererComponent(entity, componentObj);
                break;
            case nameof(AudioListenerComponent):
                AddComponent<AudioListenerComponent>(entity, componentObj);
                break;
            case nameof(AudioSourceComponent):
                DeserializeAudioSourceComponent(entity, componentObj);
                break;
            case nameof(AnimationComponent):
                AddComponent<AnimationComponent>(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
        }
    }

    private void DeserializeAudioSourceComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<AudioSourceComponent>(_defaultSerializerOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.AudioClipPath))
        {
            try
            {
                var absolutePath = Path.GetFullPath(component.AudioClipPath);
                component.AudioClip = audioEngine.LoadAudioClip(absolutePath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to load audio clip '{AudioClipPath}' for prefab entity '{EntityName}'. Audio component will be created without clip.",
                    component.AudioClipPath, entity.Name);
            }
        }

        entity.AddComponent<AudioSourceComponent>(component);
    }

    private void DeserializeNativeScriptComponent(Entity entity, JsonObject componentObj)
    {
        var component = new NativeScriptComponent();

        var scriptTypeName = componentObj[ScriptTypeKey]?.GetValue<string>();
        if (!string.IsNullOrEmpty(scriptTypeName))
        {
            component.ScriptTypeName = scriptTypeName;

            ScriptableEntity? builtInScript = scriptTypeName switch
            {
                nameof(CameraController) => new CameraController(),
                _ => null
            };

            if (builtInScript != null)
            {
                component.ScriptableEntity = builtInScript;
            }
            else
            {
                Logger.Warning(
                    "Failed to instantiate script '{ScriptTypeName}' for prefab entity '{EntityName}'. Component will retain ScriptTypeName for re-serialization.",
                    scriptTypeName, entity.Name);
            }
        }

        entity.AddComponent<NativeScriptComponent>(component);
    }

    private void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SpriteRendererComponent>(_defaultSerializerOptions);
        if (component == null)
            return;

        // Support old format: "Texture": { "Path": "..." }
        if (string.IsNullOrWhiteSpace(component.TexturePath)
            && componentObj["Texture"] is JsonObject textureObj
            && textureObj["Path"] is JsonValue pathValue)
        {
            component.TexturePath = pathValue.GetValue<string>();
        }

        if (!string.IsNullOrWhiteSpace(component.TexturePath))
        {
            component.Texture = textureFactory.Create(component.TexturePath);
        }

        entity.AddComponent<SpriteRendererComponent>(component);
    }

    private void DeserializeSubTextureRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SubTextureRendererComponent>(_defaultSerializerOptions);
        if (component == null)
            return;

        // Support old format: "Texture": { "Path": "..." }
        if (string.IsNullOrWhiteSpace(component.TexturePath)
            && componentObj["Texture"] is JsonObject textureObj
            && textureObj["Path"] is JsonValue pathValue)
        {
            component.TexturePath = pathValue.GetValue<string>();
        }

        if (!string.IsNullOrWhiteSpace(component.TexturePath))
        {
            component.Texture = textureFactory.Create(component.TexturePath);
        }

        entity.AddComponent<SubTextureRendererComponent>(component);
    }

    private void DeserializeModelRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<ModelRendererComponent>(_defaultSerializerOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.OverrideTexturePath))
        {
            component.OverrideTexture = textureFactory.Create(component.OverrideTexturePath);
        }

        entity.AddComponent<ModelRendererComponent>(component);
    }

    private void DeserializeMeshComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<MeshComponent>(_defaultSerializerOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.MeshPath))
        {
            try
            {
                component.Mesh = meshFactory.Create(component.MeshPath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex,
                    "Failed to load mesh '{MeshPath}' for prefab entity '{EntityName}'. Mesh component will be created without mesh.",
                    component.MeshPath, entity.Name);
            }
        }

        entity.AddComponent<MeshComponent>(component);
    }

    private void AddComponent<T>(Entity entity, JsonObject componentObj) where T : class, IComponent
    {
        var component = componentObj.Deserialize<T>(_defaultSerializerOptions);
        if (component != null)
        {
            entity.AddComponent<T>(component);
        }
    }

    private static JsonArray GetJsonArray(JsonObject jsonObject, string key)
    {
        if (!jsonObject.ContainsKey(key))
            throw new InvalidSceneJsonException($"Missing required key '{key}' in prefab");

        return jsonObject[key] as JsonArray ??
               throw new InvalidSceneJsonException($"'{key}' must be a JSON array in prefab");
    }
}
