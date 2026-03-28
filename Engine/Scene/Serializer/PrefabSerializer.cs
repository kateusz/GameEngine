using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Scene.Components;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code")]
internal sealed class PrefabSerializer(
    ComponentDeserializer componentDeserializer,
    SerializerOptions serializerOptions) : IPrefabSerializer
{
    private const string PrefabKey = "Prefab";
    private const string PrefabVersion = "1.0";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string VersionKey = "Version";
    private const string PrefabAssetsDirectory = "assets/prefabs";

    private readonly JsonSerializerOptions _defaultSerializerOptions = serializerOptions.Options;

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

        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        foreach (var componentNode in componentsArray)
            componentDeserializer.DeserializeComponentLenient(entity,
                componentNode ?? throw new InvalidSceneJsonException("Got null JSON Component in prefab"));
    }

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
            componentDeserializer.DeserializeComponentLenient(entity,
                componentNode ?? throw new InvalidSceneJsonException("Got null JSON Component in prefab"));

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
        SerializeComponent<AudioSourceComponent>(entity, componentsArray, nameof(AudioSourceComponent));
        SerializeComponent<LightingComponent>(entity, componentsArray, nameof(LightingComponent));
        componentDeserializer.SerializeNativeScriptComponentToArray(entity, componentsArray);
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

    private static void ClearEntityComponents(Entity entity)
    {
        if (entity.HasComponent<TransformComponent>()) entity.RemoveComponent<TransformComponent>();
        if (entity.HasComponent<CameraComponent>()) entity.RemoveComponent<CameraComponent>();
        if (entity.HasComponent<SpriteRendererComponent>()) entity.RemoveComponent<SpriteRendererComponent>();
        if (entity.HasComponent<SubTextureRendererComponent>()) entity.RemoveComponent<SubTextureRendererComponent>();
        if (entity.HasComponent<RigidBody2DComponent>()) entity.RemoveComponent<RigidBody2DComponent>();
        if (entity.HasComponent<BoxCollider2DComponent>()) entity.RemoveComponent<BoxCollider2DComponent>();
        if (entity.HasComponent<MeshComponent>()) entity.RemoveComponent<MeshComponent>();
        if (entity.HasComponent<ModelRendererComponent>()) entity.RemoveComponent<ModelRendererComponent>();
        if (entity.HasComponent<AudioListenerComponent>()) entity.RemoveComponent<AudioListenerComponent>();
        if (entity.HasComponent<AudioSourceComponent>()) entity.RemoveComponent<AudioSourceComponent>();
        if (entity.HasComponent<AnimationComponent>()) entity.RemoveComponent<AnimationComponent>();
        if (entity.HasComponent<NativeScriptComponent>()) entity.RemoveComponent<NativeScriptComponent>();
        if (entity.HasComponent<LightingComponent>()) entity.RemoveComponent<LightingComponent>();
    }

    private static JsonArray GetJsonArray(JsonObject jsonObject, string key)
    {
        if (!jsonObject.ContainsKey(key))
            throw new InvalidSceneJsonException($"Missing required key '{key}' in prefab");

        return jsonObject[key] as JsonArray ??
               throw new InvalidSceneJsonException($"'{key}' must be a JSON array in prefab");
    }
}
