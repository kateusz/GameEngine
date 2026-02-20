using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ECS;
using Engine.Audio;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Engine.Scripting;
using Serilog;
using ZLinq;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class SceneSerializer(IAudioEngine audioEngine, IScriptEngine scriptEngine, ITextureFactory textureFactory) : ISceneSerializer
{
    private static readonly ILogger Logger = Log.ForContext<SceneSerializer>();

    private const string SceneKey = "Scene";
    private const string EntitiesKey = "Entities";
    private const string DefaultSceneName = "default";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string IdKey = "Id";
    private const string ScriptTypeKey = "ScriptType";

    // TODO: this is duplicated in AnimationComponentEditor
    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new RectangleConverter(),
            new JsonStringEnumConverter()
        }
    };

    /// <summary>
    /// Serializes a scene to a JSON file at the specified path.
    /// </summary>
    /// <param name="scene">The scene to serialize.</param>
    /// <param name="path">The file path where the scene will be saved.</param>
    /// <exception cref="InvalidSceneJsonException">Thrown when the file cannot be written due to I/O errors or access restrictions.</exception>
    public void Serialize(IScene scene, string path)
    {
        var jsonObj = new JsonObject
        {
            [SceneKey] = DefaultSceneName,
            [EntitiesKey] = new JsonArray()
        };

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);

        foreach (var entity in scene.Entities)
        {
            SerializeEntity(jsonEntities, entity);
        }

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });

        try
        {
            // Use more efficient directory creation and file writing
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, jsonString);
        }
        catch (IOException ex)
        {
            throw new InvalidSceneJsonException($"Failed to write scene to {path}: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidSceneJsonException($"Access denied writing to {path}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes a scene from a JSON file at the specified path.
    /// </summary>
    /// <param name="scene">The scene to populate with deserialized entities.</param>
    /// <param name="path">The file path from which to load the scene.</param>
    /// <exception cref="InvalidSceneJsonException">Thrown when the file cannot be read, doesn't exist, or contains invalid JSON.</exception>
    public void Deserialize(IScene scene, string path)
    {
        if (!File.Exists(path))
            throw new InvalidSceneJsonException($"Scene file not found: {path}");

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new InvalidSceneJsonException($"Failed to read scene from {path}: {ex.Message}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidSceneJsonException($"Access denied reading from {path}: {ex.Message}", ex);
        }

        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidSceneJsonException("Scene file is empty or contains only whitespace");

        JsonNode? parsedNode;
        try
        {
            parsedNode = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidSceneJsonException($"Invalid JSON format: {ex.Message}", ex);
        }

        var jsonObj = parsedNode?.AsObject() ??
                      throw new InvalidSceneJsonException("Invalid JSON format - could not parse as JSON object");

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);

        foreach (var jsonEntity in jsonEntities)
        {
            if (jsonEntity is not JsonObject entityObj) continue;

            var entity = DeserializeEntity(entityObj);
            scene.AddEntity(entity);
        }
    }

    private JsonArray GetJsonArray(JsonNode jsonObject, string key)
    {
        if (!jsonObject.AsObject().ContainsKey(key))
            throw new InvalidSceneJsonException($"Missing required '{key}' key in JSON");

        return jsonObject[key] as JsonArray ??
               throw new InvalidSceneJsonException($"'{key}' must be a JSON array");
    }

    private Entity DeserializeEntity(JsonObject entityObj)
    {
        var entityId = entityObj[IdKey]?.GetValue<int>() ?? throw new InvalidSceneJsonException("Invalid entity ID");
        var entityName = entityObj[NameKey]?.GetValue<string>() ??
                         throw new InvalidSceneJsonException("Invalid entity Name");

        var entity = Entity.Create(entityId, entityName);
        var componentsArray = GetJsonArray(entityObj, ComponentsKey);

        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity,
                componentNode ?? throw new InvalidSceneJsonException("Got null JSON Component"));
        }

        return entity;
    }

    private void DeserializeComponent(Entity entity, JsonNode componentNode)
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
            case nameof(AnimationComponent):
                AddComponent<AnimationComponent>(entity, componentObj);
                break;
            case nameof(NativeScriptComponent):
                DeserializeNativeScriptComponent(entity, componentObj);
                break;
            default:
                throw new InvalidSceneJsonException($"Unknown component type: {componentName}");
        }
    }

    private void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        // Extract texture path separately to avoid abstract class deserialization issues
        string? texturePath = null;
        if (componentObj.ContainsKey("Texture") && componentObj["Texture"] is JsonObject textureObj
                                                && textureObj.ContainsKey("Path") &&
                                                textureObj["Path"] is JsonValue pathValue)
        {
            texturePath = pathValue.GetValue<string>();
        }

        var component =
            JsonSerializer.Deserialize<SpriteRendererComponent>(componentObj.ToJsonString(), DefaultSerializerOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(texturePath))
        {
            component.Texture = textureFactory.Create(texturePath);
        }

        entity.AddComponent<SpriteRendererComponent>(component);
    }

    private void DeserializeSubTextureRendererComponent(Entity entity, JsonObject componentObj)
    {
        // Extract texture path separately to avoid abstract class deserialization issues
        string? texturePath = null;
        if (componentObj.ContainsKey("Texture") && componentObj["Texture"] is JsonObject textureObj
                                                && textureObj.ContainsKey("Path") &&
                                                textureObj["Path"] is JsonValue pathValue)
        {
            texturePath = pathValue.GetValue<string>();
        }

        var component =
            JsonSerializer.Deserialize<SubTextureRendererComponent>(componentObj.ToJsonString(),
                DefaultSerializerOptions);
        if (component == null)
            return;

        // Reload texture from disk if path exists
        if (!string.IsNullOrWhiteSpace(texturePath))
        {
            component.Texture = textureFactory.Create(texturePath);
        }

        entity.AddComponent<SubTextureRendererComponent>(component);
    }

    private void DeserializeAudioSourceComponent(Entity entity, JsonObject componentObj)
    {
        // Extract the AudioClip path separately to avoid interface deserialization issues
        string? audioClipPath = null;
        if (componentObj.ContainsKey("AudioClipPath") && componentObj["AudioClipPath"] is JsonValue pathValue)
        {
            audioClipPath = pathValue.GetValue<string>();
        }

        var component =
            JsonSerializer.Deserialize<AudioSourceComponent>(componentObj.ToJsonString(), DefaultSerializerOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(audioClipPath))
        {
            try
            {
                component.AudioClip = audioEngine.LoadAudioClip(audioClipPath);
            }
            catch (Exception ex)
            {
                // Log error but continue scene deserialization
                Logger.Warning(ex,
                    "Failed to load audio clip '{AudioClipPath}' for entity '{EntityName}'. Audio component will be created without clip.",
                    audioClipPath, entity.Name);
            }
        }

        entity.AddComponent<AudioSourceComponent>(component);
    }

    private void DeserializeNativeScriptComponent(Entity entity, JsonObject componentObj)
    {
        if (!componentObj.ContainsKey(ScriptTypeKey))
        {
            // If no script type is specified, just add an empty NativeScriptComponent
            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent());
            return;
        }

        var scriptTypeName = componentObj[ScriptTypeKey]?.GetValue<string>();
        if (string.IsNullOrEmpty(scriptTypeName))
        {
            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent());
            return;
        }

        // First try to create script instance using ScriptEngine (for dynamic scripts)
        var scriptInstanceResult = scriptEngine.CreateScriptInstance(scriptTypeName);
        if (scriptInstanceResult.IsSuccess)
        {
            var scriptInstance = scriptInstanceResult.Value;
            if (componentObj["Fields"] is JsonObject fieldsObj)
            {
                foreach (var field in fieldsObj)
                {
                    var fieldName = field.Key;
                    var fieldValueNode = field.Value;
                    if (fieldValueNode != null)
                    {
                        var exposed = scriptInstance
                            .GetExposedFields()
                            .AsValueEnumerable()
                            .FirstOrDefault(f => f.Name == fieldName);
                        if (exposed.Name != null)
                        {
                            var value = fieldValueNode.Deserialize(exposed.Type, DefaultSerializerOptions);
                            scriptInstance.SetFieldValue(fieldName, value);
                        }
                    }
                }
            }

            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
            {
                ScriptableEntity = scriptInstance
            });
            return;
        }

        // If script creation fails, add empty component and log warning
        entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent());
    }

    private void AddComponent<T>(Entity entity, JsonObject componentObj) where T : class, IComponent
    {
        var component = JsonSerializer.Deserialize<T>(componentObj.ToJsonString(), DefaultSerializerOptions);
        if (component != null)
        {
            entity.AddComponent<T>(component);
        }
    }

    private void SerializeEntity(JsonArray jsonEntities, Entity entity)
    {
        var entityObj = new JsonObject
        {
            [IdKey] = entity.Id,
            [NameKey] = entity.Name,
            [ComponentsKey] = new JsonArray()
        };

        SerializeComponent<TransformComponent>(entity, entityObj, nameof(TransformComponent));
        SerializeComponent<CameraComponent>(entity, entityObj, nameof(CameraComponent));
        SerializeComponent<SpriteRendererComponent>(entity, entityObj, nameof(SpriteRendererComponent));
        SerializeComponent<SubTextureRendererComponent>(entity, entityObj, nameof(SubTextureRendererComponent));
        SerializeComponent<RigidBody2DComponent>(entity, entityObj, nameof(RigidBody2DComponent));
        SerializeComponent<BoxCollider2DComponent>(entity, entityObj, nameof(BoxCollider2DComponent));
        SerializeComponent<AudioListenerComponent>(entity, entityObj, nameof(AudioListenerComponent));
        SerializeComponent<AnimationComponent>(entity, entityObj, nameof(AnimationComponent));
        SerializeAudioSourceComponent(entity, entityObj);
        SerializeNativeScriptComponent(entity, entityObj);

        jsonEntities.Add(entityObj);
    }

    private void SerializeAudioSourceComponent(Entity entity, JsonObject entityObj)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
            return;

        var component = entity.GetComponent<AudioSourceComponent>();
        var element = JsonSerializer.SerializeToNode(component, DefaultSerializerOptions);
        if (element != null)
        {
            element[NameKey] = nameof(AudioSourceComponent);
            var components = GetJsonArray(entityObj, ComponentsKey);
            components.Add(element);
        }
    }

    private void SerializeNativeScriptComponent(Entity entity, JsonObject entityObj)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        var scriptComponentObj = new JsonObject
        {
            [NameKey] = nameof(NativeScriptComponent)
        };

        // Store the script type name if a script is attached
        if (component.ScriptableEntity != null)
        {
            var scriptTypeName = component.ScriptableEntity.GetType().Name;
            scriptComponentObj[ScriptTypeKey] = scriptTypeName;

            // --- Serialize public fields/properties ---
            var fieldsObj = new JsonObject();
            foreach (var (fieldName, fieldType, fieldValue) in component.ScriptableEntity.GetExposedFields())
            {
                fieldsObj[fieldName] = JsonSerializer.SerializeToNode(fieldValue, DefaultSerializerOptions);
            }

            if (fieldsObj.Count > 0)
                scriptComponentObj["Fields"] = fieldsObj;
            // --- End serialize fields ---
        }

        var components = GetJsonArray(entityObj, ComponentsKey);
        components.Add(scriptComponentObj);
    }

    private void SerializeComponent<T>(Entity entity, JsonObject entityObj, string componentName)
        where T : IComponent
    {
        if (!entity.HasComponent<T>())
            return;

        var component = entity.GetComponent<T>();
        var element = JsonSerializer.SerializeToNode(component, DefaultSerializerOptions);
        if (element != null)
        {
            element[NameKey] = componentName;
            var components = GetJsonArray(entityObj, ComponentsKey);
            components.Add(element);
        }
    }
}
