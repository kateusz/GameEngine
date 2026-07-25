using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class SceneSerializer(
    ComponentSerializerRegistry registry,
    SerializerOptions serializerOptions) : ISceneSerializer
{
    private const string SceneKey = "Scene";
    private const string BackgroundColorKey = "BackgroundColor";
    private const string DimensionKey = "Dimension";
    private const string EntitiesKey = "Entities";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string IdKey = "Id";

    private readonly JsonSerializerOptions _options = serializerOptions.Options;

    public void Serialize(IScene scene, string path)
    {
        var sceneName = Path.GetFileNameWithoutExtension(path);
        var jsonObj = new JsonObject
        {
            [SceneKey] = sceneName,
            [BackgroundColorKey] = JsonSerializer.SerializeToNode(scene.BackgroundColor, _options),
            [DimensionKey] = JsonSerializer.SerializeToNode(scene.Dimension, _options),
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

        if (jsonObj.TryGetPropertyValue(BackgroundColorKey, out var backgroundColorNode) && backgroundColorNode != null)
            scene.BackgroundColor = backgroundColorNode.Deserialize<Vector4>(_options)!;

        if (jsonObj.TryGetPropertyValue(DimensionKey, out var dimensionNode) && dimensionNode != null)
            scene.Dimension = dimensionNode.Deserialize<SceneDimension>(_options)!;

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);
        foreach (var jsonEntity in jsonEntities)
        {
            if (jsonEntity is not JsonObject entityObj) continue;
            scene.AddEntity(DeserializeEntity(entityObj));
        }

        scene.RebuildHierarchyIndex();
    }

    private static JsonArray GetJsonArray(JsonNode jsonObject, string key)
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
            if (componentNode is not JsonObject componentObj)
                throw new InvalidSceneJsonException("Got null JSON Component");

            registry.DeserializeComponent(entity, componentObj, _options, strict: true);
        }

        return entity;
    }

    private void SerializeEntity(JsonArray jsonEntities, Entity entity)
    {
        var componentsArray = new JsonArray();
        registry.SerializeEntity(entity, componentsArray, _options);

        jsonEntities.Add(new JsonObject
        {
            [IdKey] = entity.Id,
            [NameKey] = entity.Name,
            [ComponentsKey] = componentsArray
        });
    }
}
