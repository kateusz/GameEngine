using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Audio;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class PrefabSerializer : EntitySerializerBase, IPrefabSerializer
{
    private new static readonly ILogger Logger = Log.ForContext<PrefabSerializer>();

    private const string PrefabKey = "Prefab";
    private const string PrefabVersion = "1.0";
    private const string IdKey = "Id";
    private const string VersionKey = "Version";
    private const string PrefabAssetsDirectory = "assets/prefabs";

    public PrefabSerializer(IAudioEngine audioEngine) : base(audioEngine)
    {
    }

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
                      throw new InvalidOperationException("Invalid prefab JSON");

        // Clear existing components (except ID and Name)
        ClearEntityComponents(entity);

        // Apply prefab components
        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity, componentNode ??
                                         throw new InvalidOperationException("Got null JSON Component"));
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
                      throw new InvalidOperationException("Invalid prefab JSON");

        var entity = Entity.Create(entityId, entityName);

        var componentsArray = GetJsonArray(jsonObj, ComponentsKey);
        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity, componentNode ??
                                         throw new InvalidOperationException("Got null JSON Component"));
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
        SerializeAudioSourceComponent(entity, componentsArray);
        SerializeNativeScriptComponent(entity, componentsArray);
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
        if (entity.HasComponent<NativeScriptComponent>())
            entity.RemoveComponent<NativeScriptComponent>();
    }
}
