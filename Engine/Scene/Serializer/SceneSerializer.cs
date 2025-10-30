using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Audio;
using Engine.Renderer.Textures;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class SceneSerializer : EntitySerializerBase, ISceneSerializer
{
    private new static readonly ILogger Logger = Log.ForContext<SceneSerializer>();

    private const string SceneKey = "Scene";
    private const string EntitiesKey = "Entities";
    private const string DefaultSceneName = "default";
    private const string AssetsDirectory = "assets/scenes";
    private const string IdKey = "Id";

    public SceneSerializer(IAudioEngine audioEngine) : base(audioEngine)
    {
    }

    /// <summary>
    /// Serializes a scene to a JSON file at the specified path.
    /// </summary>
    /// <param name="scene">The scene to serialize.</param>
    /// <param name="path">The file path where the scene will be saved.</param>
    /// <exception cref="InvalidSceneJsonException">Thrown when the file cannot be written due to I/O errors or access restrictions.</exception>
    public void Serialize(Scene scene, string path)
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
    public void Deserialize(Scene scene, string path)
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

    /// <summary>
    /// Override to throw InvalidSceneJsonException instead of InvalidOperationException.
    /// </summary>
    protected new JsonArray GetJsonArray(JsonNode jsonObject, string key)
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

    /// <summary>
    /// Override to use TextureFactory for loading textures (scene-specific).
    /// </summary>
    protected override void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = JsonSerializer.Deserialize<SpriteRendererComponent>(componentObj, SerializationConfig.DefaultOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.Texture?.Path))
        {
            component.Texture = TextureFactory.Create(component.Texture.Path);
        }

        entity.AddComponent<SpriteRendererComponent>(component);
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
        SerializeAudioSourceComponent(entity, entityObj);
        SerializeNativeScriptComponent(entity, entityObj);

        jsonEntities.Add(entityObj);
    }
}