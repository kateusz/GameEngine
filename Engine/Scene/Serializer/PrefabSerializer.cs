using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class PrefabSerializer(
    ComponentSerializerRegistry registry,
    SerializerOptions serializerOptions) : IPrefabSerializer
{
    private const string PrefabKey = "Prefab";
    private const string PrefabVersion = "1.0";
    private const string ComponentsKey = "Components";
    private const string VersionKey = "Version";
    private const string PrefabAssetsDirectory = "assets/prefabs";

    private readonly JsonSerializerOptions _options = serializerOptions.Options;

    public void SerializeToPrefab(Entity entity, string prefabName, string projectPath)
    {
        var prefabDir = Path.Combine(projectPath, PrefabAssetsDirectory);
        Directory.CreateDirectory(prefabDir);

        var prefabPath = Path.Combine(prefabDir, $"{prefabName}.prefab");

        var componentsArray = new JsonArray();
        registry.SerializeEntity(entity, componentsArray, _options);

        var jsonObj = new JsonObject
        {
            [PrefabKey] = prefabName,
            [VersionKey] = PrefabVersion,
            ["OriginalName"] = entity.Name,
            [ComponentsKey] = componentsArray
        };

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(prefabPath, jsonString);
    }

    public void ApplyPrefabToEntity(Entity entity, string prefabPath)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var json = File.ReadAllText(prefabPath);
        var jsonObj = JsonNode.Parse(json)?.AsObject() ??
                      throw new InvalidSceneJsonException($"Invalid prefab JSON in {prefabPath}");

        ClearEntityComponents(entity);
        DeserializeComponents(entity, GetJsonArray(jsonObj, ComponentsKey), strict: false);
    }

    public Entity CreateEntityFromPrefab(string prefabPath, string entityName, int entityId)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var json = File.ReadAllText(prefabPath);
        var jsonObj = JsonNode.Parse(json)?.AsObject() ??
                      throw new InvalidSceneJsonException($"Invalid prefab JSON in {prefabPath}");

        var entity = Entity.Create(entityId, entityName);
        DeserializeComponents(entity, GetJsonArray(jsonObj, ComponentsKey), strict: false);
        return entity;
    }

    private void DeserializeComponents(Entity entity, JsonArray componentsArray, bool strict)
    {
        foreach (var componentNode in componentsArray)
        {
            if (componentNode is not JsonObject componentObj)
                throw new InvalidSceneJsonException("Got null JSON Component in prefab");

            registry.DeserializeComponent(entity, componentObj, _options, strict);
        }
    }

    private static void ClearEntityComponents(Entity entity)
    {
        foreach (var component in entity.GetAllComponents().ToList())
            entity.RemoveComponent(component.GetType());
    }

    private static JsonArray GetJsonArray(JsonObject jsonObject, string key)
    {
        if (!jsonObject.ContainsKey(key))
            throw new InvalidSceneJsonException($"Missing required key '{key}' in prefab");

        return jsonObject[key] as JsonArray ??
               throw new InvalidSceneJsonException($"'{key}' must be a JSON array in prefab");
    }
}
