using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using SceneComponents;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class PrefabSerializer(
    ComponentSerializerRegistry registry,
    SerializerOptions serializerOptions) : IPrefabSerializer
{
    private const string PrefabKey = "Prefab";
    private const string PrefabVersionV1 = "1.0";
    private const string PrefabVersionV2 = "2.0";
    private const string ComponentsKey = "Components";
    private const string EntitiesKey = "Entities";
    private const string VersionKey = "Version";
    private const string PrefabIndexKey = "PrefabIndex";
    private const string RootPrefabIndexKey = "RootPrefabIndex";
    private const string NameKey = "Name";
    private const string PrefabAssetsDirectory = "assets/prefabs";

    private readonly JsonSerializerOptions _options = serializerOptions.Options;

    public void SerializeToPrefab(IScene scene, Entity entity, string prefabName, string projectPath)
    {
        var prefabDir = Path.Combine(projectPath, PrefabAssetsDirectory);
        Directory.CreateDirectory(prefabDir);

        var prefabPath = Path.Combine(prefabDir, $"{prefabName}.prefab");

        var subtree = scene.CollectSubtree(entity);
        var idToIndex = new Dictionary<int, int>(subtree.Count);
        for (var i = 0; i < subtree.Count; i++)
            idToIndex[subtree[i].Id] = i;

        var entitiesArray = new JsonArray();
        for (var i = 0; i < subtree.Count; i++)
        {
            var source = subtree[i];
            var componentsArray = new JsonArray();
            registry.SerializeEntity(source, componentsArray, _options);

            // Remap ParentId scene Ids → prefab-local indices
            foreach (var node in componentsArray)
            {
                if (node is not JsonObject compObj)
                    continue;
                if (compObj["Name"]?.GetValue<string>() != nameof(ParentComponent))
                    continue;

                if (compObj["ParentId"] is JsonValue parentVal && parentVal.TryGetValue<int>(out var parentSceneId))
                {
                    if (!idToIndex.TryGetValue(parentSceneId, out var parentIndex))
                        throw new InvalidSceneJsonException(
                            $"Prefab save failed: ParentId {parentSceneId} is outside the saved subtree");
                    compObj["ParentId"] = parentIndex;
                }
            }

            entitiesArray.Add(new JsonObject
            {
                [PrefabIndexKey] = i,
                [NameKey] = source.Name,
                [ComponentsKey] = componentsArray
            });
        }

        var jsonObj = new JsonObject
        {
            [PrefabKey] = prefabName,
            [VersionKey] = PrefabVersionV2,
            [RootPrefabIndexKey] = 0,
            [EntitiesKey] = entitiesArray
        };

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(prefabPath, jsonString);
    }

    public void ApplyPrefabToEntity(IScene scene, Entity entity, string prefabPath)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var jsonObj = ParsePrefab(prefabPath);
        var version = jsonObj[VersionKey]?.GetValue<string>() ?? PrefabVersionV1;

            if (version.StartsWith('2'))
        {
            var savedParent = scene.GetParent(entity);

            foreach (var child in scene.GetChildren(entity).ToList())
                scene.DestroyEntity(child);

            ClearEntityComponents(entity);
            InstantiateV2Onto(scene, jsonObj, rootEntity: entity);

            if (savedParent is not null)
                scene.SetParent(entity, savedParent);
            return;
        }

        // v1: replace components on this entity only
        ClearEntityComponents(entity);
        DeserializeComponents(entity, GetJsonArray(jsonObj, ComponentsKey), strict: false);
        scene.RebuildHierarchyIndex();
    }

    public Entity CreateEntityFromPrefab(IScene scene, string prefabPath, string? entityName = null)
    {
        if (!File.Exists(prefabPath))
            throw new FileNotFoundException($"Prefab file not found: {prefabPath}");

        var jsonObj = ParsePrefab(prefabPath);
        var version = jsonObj[VersionKey]?.GetValue<string>() ?? PrefabVersionV1;

        if (version.StartsWith('2'))
            return InstantiateV2Onto(scene, jsonObj, rootEntity: null, entityName);

        var name = entityName
                   ?? jsonObj["OriginalName"]?.GetValue<string>()
                   ?? jsonObj[PrefabKey]?.GetValue<string>()
                   ?? "Prefab";
        var entity = scene.CreateEntity(name);
        DeserializeComponents(entity, GetJsonArray(jsonObj, ComponentsKey), strict: false);
        scene.RebuildHierarchyIndex();
        return entity;
    }

    private Entity InstantiateV2Onto(IScene scene, JsonObject jsonObj, Entity? rootEntity, string? rootNameOverride = null)
    {
        var entitiesArray = GetJsonArray(jsonObj, EntitiesKey);
        var rootIndex = jsonObj[RootPrefabIndexKey]?.GetValue<int>() ?? 0;
        if (rootIndex < 0 || rootIndex >= entitiesArray.Count)
            throw new InvalidSceneJsonException($"Invalid RootPrefabIndex {rootIndex}");

        var indexToEntity = new Dictionary<int, Entity>(entitiesArray.Count);

        for (var i = 0; i < entitiesArray.Count; i++)
        {
            if (entitiesArray[i] is not JsonObject entityObj)
                throw new InvalidSceneJsonException("Prefab entity entry must be a JSON object");

            var prefabIndex = entityObj[PrefabIndexKey]?.GetValue<int>() ?? i;
            var name = entityObj[NameKey]?.GetValue<string>() ?? $"Entity_{prefabIndex}";

            Entity entity;
            if (i == rootIndex && rootEntity is not null)
            {
                entity = rootEntity;
                if (rootNameOverride is not null)
                    entity.Name = rootNameOverride;
                else if (!string.IsNullOrWhiteSpace(name))
                    entity.Name = name;
            }
            else if (i == rootIndex)
            {
                entity = scene.CreateEntity(rootNameOverride ?? name);
            }
            else
            {
                entity = scene.CreateEntity(name);
            }

            if (entity != rootEntity)
                ClearEntityComponents(entity);

            DeserializeComponents(entity, GetJsonArray(entityObj, ComponentsKey), strict: false);
            indexToEntity[prefabIndex] = entity;
        }

        foreach (var (prefabIndex, entity) in indexToEntity)
        {
            if (!entity.TryGetComponent<ParentComponent>(out var parentComp) || parentComp.ParentId is not int parentIndex)
                continue;

            if (!indexToEntity.TryGetValue(parentIndex, out var parentEntity))
                throw new InvalidSceneJsonException(
                    $"Prefab ParentId index {parentIndex} is invalid (entity PrefabIndex {prefabIndex})");

            parentComp.ParentId = parentEntity.Id;
        }

        if (rootEntity is not null && indexToEntity.TryGetValue(rootIndex, out var mappedRoot))
        {
            if (mappedRoot.HasComponent<ParentComponent>())
                mappedRoot.RemoveComponent<ParentComponent>();
        }

        scene.RebuildHierarchyIndex();

        return indexToEntity[rootIndex];
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

    private static JsonObject ParsePrefab(string prefabPath)
    {
        string json;
        try
        {
            json = File.ReadAllText(prefabPath);
        }
        catch (IOException ex)
        {
            throw new InvalidSceneJsonException($"Failed to read prefab from {prefabPath}: {ex.Message}", ex);
        }

        return JsonNode.Parse(json)?.AsObject()
               ?? throw new InvalidSceneJsonException($"Invalid prefab JSON in {prefabPath}");
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
