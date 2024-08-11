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
    public static void Serialize(Scene scene, string path)
    {
        var jsonObj = new JsonObject
        {
            ["Scene"] = "default",
            ["Entities"] = new JsonArray()
        };

        if (jsonObj["Entities"] is not JsonArray jsonEntities) 
            throw new InvalidSceneJsonException(nameof(jsonEntities));

        foreach (var entity in Context.Instance.Entities.ToList())
        {
            SerializeEntity(jsonEntities, entity);
        }

        var jsonString = jsonObj.ToJsonString();

        Directory.CreateDirectory("assets/scenes");
        File.WriteAllText(path, jsonString);
    }

    public static void Deserialize(Scene scene, string path)
    {
        var contextEntities = Context.Instance.Entities;
        var json = File.ReadAllText(path);
        var jsonObj = JsonNode.Parse(json)?.AsObject();

        if (jsonObj is null)
            throw new InvalidSceneJsonException("Got null JSON Object from JSON");

        if (jsonObj["Entities"] is null)
            throw new InvalidSceneJsonException("Got invalid Scene JSON");

        var jsonEntities = jsonObj["Entities"]!.AsArray();

        foreach (var jsonEntity in jsonEntities)
        {
            if (jsonEntity is null)
                continue;
            
            var entityObj = jsonEntity.AsObject();

            if (entityObj["Id"] is null || entityObj["Name"] is null)
                throw new InvalidSceneJsonException("Got invalid JSON for entity");

            var entityId = entityObj["Id"]!.GetValue<Guid>();
            var entityName = entityObj["Name"]?.GetValue<string>();

            // Create a new Entity and set its Id and Name
            var entity = new Entity(entityId, entityName!);

            // TODO: remove hard-coded native script for camera
            if (entity.Name == "Camera Entity")
            {
                entity.AddComponent(new NativeScriptComponent
                {
                    ScriptableEntity = new CameraController()
                });
            }

            if (entityObj["Components"] is null or not JsonArray)
                throw new InvalidSceneJsonException($"Got invalid Components for entity {entity.Name}");
            
            // Deserialize components
            var componentsArray = entityObj["Components"]!.AsArray();
            foreach (var componentNode in componentsArray)
            {
                if (componentNode is null || componentNode["Name"] is null)
                    throw new InvalidSceneJsonException("Got invalid component JSON");
                        
                var componentName = componentNode["Name"]!.GetValue<string>();

                if (componentName == nameof(TransformComponent))
                {
                    var transformComponent = JsonSerializer.Deserialize<TransformComponent>(
                        componentNode.ToJsonString(), new JsonSerializerOptions
                        {
                            Converters =
                            {
                                new Vector3Converter()
                            }
                        });

                    if (transformComponent != null)
                    {
                        entity.AddComponent(transformComponent);
                    }
                }
                else if (componentName == nameof(CameraComponent))
                {
                    var cameraComponent = JsonSerializer.Deserialize<CameraComponent>(componentNode.ToJsonString(),
                        new JsonSerializerOptions
                        {
                            Converters =
                            {
                                new JsonStringEnumConverter(),
                                new Vector3Converter()
                            }
                        });
                    if (cameraComponent != null)
                    {
                        entity.AddComponent(cameraComponent);
                    }
                }
                else if (componentName == nameof(SpriteRendererComponent))
                {
                    var spriteRendererComponent = JsonSerializer.Deserialize<SpriteRendererComponent>(
                        componentNode.ToJsonString(), new JsonSerializerOptions
                        {
                            Converters =
                            {
                                new Vector4Converter()
                            }
                        });
                    if (spriteRendererComponent != null)
                    {
                        entity.AddComponent(spriteRendererComponent);
                    }
                }
            }

            contextEntities.Add(entity);
        }
    }

    private static void SerializeEntity(JsonArray jsonEntities, Entity entity)
    {
        var entityObj = new JsonObject
        {
            ["Id"] = entity.Id,
            ["Name"] = entity.Name,
            ["Components"] = new JsonArray()
        };

        if (entity.HasComponent<TransformComponent>())
        {
            var component = entity.GetComponent<TransformComponent>();
            var element = JsonSerializer.SerializeToNode(component, new JsonSerializerOptions
            {
                Converters =
                {
                    new Vector3Converter()
                },
                WriteIndented = true
            });
            element!["Name"] = nameof(TransformComponent);

            var components = entityObj["Components"] as JsonArray;
            components!.Add(element);
        }

        if (entity.HasComponent<CameraComponent>())
        {
            var component = entity.GetComponent<CameraComponent>();
            var element = JsonSerializer.SerializeToNode(component, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            element!["Name"] = nameof(CameraComponent);

            var components = entityObj["Components"] as JsonArray;
            components!.Add(element);
        }

        if (entity.HasComponent<SpriteRendererComponent>())
        {
            var component = entity.GetComponent<SpriteRendererComponent>();
            var element = JsonSerializer.SerializeToNode(component, new JsonSerializerOptions
            {
                Converters =
                {
                    new Vector4Converter()
                },
                WriteIndented = true
            });
            element!["Name"] = nameof(SpriteRendererComponent);

            var components = entityObj["Components"] as JsonArray;
            components!.Add(element);
        }

        jsonEntities.Add(entityObj);
    }
}