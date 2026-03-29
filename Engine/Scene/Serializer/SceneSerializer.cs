using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class SceneSerializer(
    ComponentDeserializer componentDeserializer,
    SerializerOptions serializerOptions) : ISceneSerializer
{
    private const string SceneKey = "Scene";
    private const string EntitiesKey = "Entities";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string IdKey = "Id";

    private readonly JsonSerializerOptions _defaultSerializerOptions = serializerOptions.Options;

    public void Serialize(IScene scene, string path)
    {
        var sceneName = Path.GetFileNameWithoutExtension(path);
        var jsonObj = new JsonObject
        {
            [SceneKey] = sceneName,
            [EntitiesKey] = new JsonArray()
        };

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);
        foreach (var entity in scene.Entities)
            SerializeEntity(jsonEntities, entity);

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

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
            componentDeserializer.DeserializeComponent(entity,
                componentNode ?? throw new InvalidSceneJsonException("Got null JSON Component"));

        return entity;
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
        SerializeComponent<MeshComponent>(entity, entityObj, nameof(MeshComponent));
        SerializeComponent<ModelRendererComponent>(entity, entityObj, nameof(ModelRendererComponent));
        SerializeComponent<AnimationComponent>(entity, entityObj, nameof(AnimationComponent));
        SerializeComponent<AudioSourceComponent>(entity, entityObj, nameof(AudioSourceComponent));
        SerializeComponent<LightingComponent>(entity, entityObj, nameof(LightingComponent));
        componentDeserializer.SerializeNativeScriptComponent(entity, entityObj, ComponentsKey);

        jsonEntities.Add(entityObj);
    }

    private void SerializeComponent<T>(Entity entity, JsonObject entityObj, string componentName)
        where T : IComponent
    {
        if (!entity.HasComponent<T>())
            return;

        var component = entity.GetComponent<T>();
        var element = JsonSerializer.SerializeToNode(component, _defaultSerializerOptions);
        if (element != null)
        {
            element[NameKey] = componentName;
            var components = GetJsonArray(entityObj, ComponentsKey);
            components.Add(element);
        }
    }
}
