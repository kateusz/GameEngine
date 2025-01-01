using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ECS;
using Engine.Scene.Components;

namespace Engine.Scene.Serializer;

[SuppressMessage("AOT",
    "IL3050:Calling members annotated with \'RequiresDynamicCodeAttribute\' may break functionality when AOT compiling.")]
[SuppressMessage("Trimming",
    "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class SceneSerializer
{
    private const string SceneKey = "Scene";
    private const string EntitiesKey = "Entities";
    private const string DefaultSceneName = "default";
    private const string AssetsDirectory = "assets/scenes";
    private const string ComponentsKey = "Components";
    private const string NameKey = "Name";
    private const string IdKey = "Id";

    private static readonly JsonSerializerOptions DefaultSerializerOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new JsonStringEnumConverter()
        }
    };

    public static void Serialize(Scene scene, string path)
    {
        var jsonObj = new JsonObject
        {
            [SceneKey] = DefaultSceneName,
            [EntitiesKey] = new JsonArray()
        };

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);

        foreach (var entity in scene.Entities.ToList())
        {
            SerializeEntity(jsonEntities, entity);
        }

        var jsonString = jsonObj.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Directory.CreateDirectory(AssetsDirectory);
        File.WriteAllText(path, jsonString);
    }

    public static void Deserialize(Scene scene, string path)
    {
        var json = File.ReadAllText(path);
        var jsonObj = JsonNode.Parse(json)?.AsObject() ??
                      throw new InvalidSceneJsonException("Got null JSON Object from JSON");

        var jsonEntities = GetJsonArray(jsonObj, EntitiesKey);

        foreach (var jsonEntity in jsonEntities)
        {
            if (jsonEntity is not JsonObject entityObj) continue;

            var entity = DeserializeEntity(entityObj);
            scene.AddEntity(entity);
        }
    }

    private static JsonArray GetJsonArray(JsonNode jsonObject, string key)
    {
        return jsonObject[key] as JsonArray ?? throw new InvalidSceneJsonException($"Got invalid {key} JSON");
    }

    private static Entity DeserializeEntity(JsonObject entityObj)
    {
        var entityId = entityObj[IdKey]?.GetValue<int>() ?? throw new InvalidSceneJsonException("Invalid entity ID");
        var entityName = entityObj[NameKey]?.GetValue<string>() ??
                         throw new InvalidSceneJsonException("Invalid entity Name");

        var entity = Entity.Create(entityId, entityName);

        // Handle special cases for entity names
        // TODO: NativeScriptComponent-  remove hardcoded logic
        if (entity.Name == "Primary Camera")
        {
            entity.AddComponent(new NativeScriptComponent
            {
                ScriptableEntity = new CameraController()
            });
        }

        var componentsArray = GetJsonArray(entityObj, ComponentsKey);

        foreach (var componentNode in componentsArray)
        {
            DeserializeComponent(entity,
                componentNode ?? throw new InvalidSceneJsonException("Got null JSON Component"));
        }

        return entity;
    }

    private static void DeserializeComponent(Entity entity, JsonNode componentNode)
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
                AddComponent<SpriteRendererComponent>(entity, componentObj);
                break;
            case nameof(RigidBody2DComponent):
                AddComponent<RigidBody2DComponent>(entity, componentObj);
                break;
            case nameof(BoxCollider2DComponent):
                AddComponent<BoxCollider2DComponent>(entity, componentObj);
                break;
            default:
                throw new InvalidSceneJsonException($"Unknown component type: {componentName}");
        }
    }

    private static void AddComponent<T>(Entity entity, JsonObject componentObj) where T : Component
    {
        var component = JsonSerializer.Deserialize<T>(componentObj.ToJsonString(), DefaultSerializerOptions);
        if (component != null)
        {
            entity.AddComponent(component);
        }
    }

    private static void SerializeEntity(JsonArray jsonEntities, Entity entity)
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
        SerializeComponent<RigidBody2DComponent>(entity, entityObj, nameof(RigidBody2DComponent));
        SerializeComponent<BoxCollider2DComponent>(entity, entityObj, nameof(BoxCollider2DComponent));

        jsonEntities.Add(entityObj);
    }

    private static void SerializeComponent<T>(Entity entity, JsonObject entityObj, string componentName)
        where T : Component
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