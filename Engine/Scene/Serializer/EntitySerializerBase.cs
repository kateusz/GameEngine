using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using Engine.Audio;
using Engine.Scene.Components;
using Engine.Scripting;
using Serilog;
using ZLinq;

namespace Engine.Scene.Serializer;

/// <summary>
/// Base class for entity-based serialization (scenes and prefabs).
/// Provides shared component serialization/deserialization logic to eliminate code duplication.
/// </summary>
public abstract class EntitySerializerBase
{
    protected static readonly ILogger Logger = Log.ForContext<EntitySerializerBase>();
    
    protected const string ComponentsKey = "Components";
    protected const string NameKey = "Name";
    protected const string ScriptTypeKey = "ScriptType";
    protected const string FieldsKey = "Fields";
    protected const string AudioClipPathKey = "AudioClipPath";
    
    protected readonly IAudioEngine AudioEngine;
    
    private readonly Dictionary<string, Action<Entity, JsonObject>> _componentDeserializers;
    
    protected EntitySerializerBase(IAudioEngine audioEngine)
    {
        AudioEngine = audioEngine ?? throw new ArgumentNullException(nameof(audioEngine));
        
        // Initialize component deserializers map
        _componentDeserializers = new Dictionary<string, Action<Entity, JsonObject>>
        {
            [nameof(TransformComponent)] = AddComponent<TransformComponent>,
            [nameof(CameraComponent)] = AddComponent<CameraComponent>,
            [nameof(SpriteRendererComponent)] = DeserializeSpriteRendererComponent,
            [nameof(SubTextureRendererComponent)] = AddComponent<SubTextureRendererComponent>,
            [nameof(RigidBody2DComponent)] = AddComponent<RigidBody2DComponent>,
            [nameof(BoxCollider2DComponent)] = AddComponent<BoxCollider2DComponent>,
            [nameof(MeshComponent)] = AddComponent<MeshComponent>,
            [nameof(ModelRendererComponent)] = AddComponent<ModelRendererComponent>,
            [nameof(AudioListenerComponent)] = AddComponent<AudioListenerComponent>,
            [nameof(AudioSourceComponent)] = DeserializeAudioSourceComponent,
            [nameof(NativeScriptComponent)] = DeserializeNativeScriptComponent
        };
    }
    
    /// <summary>
    /// Serialize a component to a JSON array.
    /// </summary>
    protected static void SerializeComponent<T>(Entity entity, JsonArray componentsArray, string componentName)
        where T : IComponent
    {
        if (!entity.HasComponent<T>())
            return;
        
        var component = entity.GetComponent<T>();
        var element = JsonSerializer.SerializeToNode(component, SerializationConfig.DefaultOptions);
        if (element != null)
        {
            element[NameKey] = componentName;
            componentsArray.Add(element);
        }
    }
    
    /// <summary>
    /// Serialize a component to a JSON object.
    /// </summary>
    protected static void SerializeComponent<T>(Entity entity, JsonObject entityObj, string componentName)
        where T : IComponent
    {
        if (!entity.HasComponent<T>())
            return;
        
        var component = entity.GetComponent<T>();
        var element = JsonSerializer.SerializeToNode(component, SerializationConfig.DefaultOptions);
        if (element != null)
        {
            element[NameKey] = componentName;
            var components = GetJsonArray(entityObj, ComponentsKey);
            components.Add(element);
        }
    }
    
    /// <summary>
    /// Serialize AudioSourceComponent with special handling.
    /// </summary>
    protected static void SerializeAudioSourceComponent(Entity entity, JsonArray componentsArray)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
            return;

        var component = entity.GetComponent<AudioSourceComponent>();
        var element = JsonSerializer.SerializeToNode(component, SerializationConfig.DefaultOptions);
        if (element != null)
        {
            element[NameKey] = nameof(AudioSourceComponent);
            componentsArray.Add(element);
        }
    }
    
    /// <summary>
    /// Serialize AudioSourceComponent with special handling to JSON object.
    /// </summary>
    protected static void SerializeAudioSourceComponent(Entity entity, JsonObject entityObj)
    {
        if (!entity.HasComponent<AudioSourceComponent>())
            return;

        var component = entity.GetComponent<AudioSourceComponent>();
        var element = JsonSerializer.SerializeToNode(component, SerializationConfig.DefaultOptions);
        if (element != null)
        {
            element[NameKey] = nameof(AudioSourceComponent);
            var components = GetJsonArray(entityObj, ComponentsKey);
            components.Add(element);
        }
    }
    
    /// <summary>
    /// Serialize NativeScriptComponent with field serialization.
    /// </summary>
    protected static void SerializeNativeScriptComponent(Entity entity, JsonArray componentsArray)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        var scriptComponentObj = new JsonObject
        {
            [NameKey] = nameof(NativeScriptComponent)
        };

        if (component.ScriptableEntity != null)
        {
            var scriptTypeName = component.ScriptableEntity.GetType().Name;
            scriptComponentObj[ScriptTypeKey] = scriptTypeName;

            var fieldsObj = new JsonObject();
            foreach (var (fieldName, fieldType, fieldValue) in component.ScriptableEntity.GetExposedFields())
            {
                fieldsObj[fieldName] = JsonSerializer.SerializeToNode(fieldValue, SerializationConfig.DefaultOptions);
            }

            if (fieldsObj.Count > 0)
                scriptComponentObj[FieldsKey] = fieldsObj;
        }

        componentsArray.Add(scriptComponentObj);
    }
    
    /// <summary>
    /// Serialize NativeScriptComponent with field serialization to JSON object.
    /// </summary>
    protected static void SerializeNativeScriptComponent(Entity entity, JsonObject entityObj)
    {
        if (!entity.HasComponent<NativeScriptComponent>())
            return;

        var component = entity.GetComponent<NativeScriptComponent>();
        var scriptComponentObj = new JsonObject
        {
            [NameKey] = nameof(NativeScriptComponent)
        };

        if (component.ScriptableEntity != null)
        {
            var scriptTypeName = component.ScriptableEntity.GetType().Name;
            scriptComponentObj[ScriptTypeKey] = scriptTypeName;

            var fieldsObj = new JsonObject();
            foreach (var (fieldName, fieldType, fieldValue) in component.ScriptableEntity.GetExposedFields())
            {
                fieldsObj[fieldName] = JsonSerializer.SerializeToNode(fieldValue, SerializationConfig.DefaultOptions);
            }

            if (fieldsObj.Count > 0)
                scriptComponentObj[FieldsKey] = fieldsObj;
        }

        var components = GetJsonArray(entityObj, ComponentsKey);
        components.Add(scriptComponentObj);
    }
    
    /// <summary>
    /// Deserialize a component from JSON.
    /// </summary>
    protected void DeserializeComponent(Entity entity, JsonNode componentNode)
    {
        if (componentNode is not JsonObject componentObj || componentObj[NameKey] is null)
            throw new InvalidOperationException("Invalid component JSON");

        var componentName = componentObj[NameKey]!.GetValue<string>();

        if (_componentDeserializers.TryGetValue(componentName, out var deserializer))
        {
            deserializer(entity, componentObj);
        }
        else
        {
            Logger.Warning("Unknown component type: {ComponentName}", componentName);
        }
    }
    
    /// <summary>
    /// Deserialize SpriteRendererComponent with texture loading.
    /// </summary>
    protected virtual void DeserializeSpriteRendererComponent(Entity entity, JsonObject componentObj)
    {
        var component = componentObj.Deserialize<SpriteRendererComponent>(SerializationConfig.DefaultOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(component.Texture?.Path))
        {
            component.Texture = Engine.Renderer.Textures.TextureFactory.Create(component.Texture.Path);
        }

        entity.AddComponent<SpriteRendererComponent>(component);
    }
    
    /// <summary>
    /// Deserialize AudioSourceComponent with audio clip loading.
    /// </summary>
    protected void DeserializeAudioSourceComponent(Entity entity, JsonObject componentObj)
    {
        // Extract the AudioClip path separately to avoid interface deserialization issues
        string? audioClipPath = null;
        if (componentObj.ContainsKey(AudioClipPathKey) && componentObj[AudioClipPathKey] is JsonValue pathValue)
        {
            audioClipPath = pathValue.GetValue<string>();
        }

        var component = componentObj.Deserialize<AudioSourceComponent>(SerializationConfig.DefaultOptions);
        if (component == null)
            return;

        if (!string.IsNullOrWhiteSpace(audioClipPath))
        {
            try
            {
                component.AudioClip = AudioEngine.LoadAudioClip(audioClipPath);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, 
                    "Failed to load audio clip '{AudioClipPath}' for entity '{EntityName}'. Audio component will be created without clip.", 
                    audioClipPath, entity.Name);
            }
        }

        entity.AddComponent<AudioSourceComponent>(component);
    }
    
    /// <summary>
    /// Deserialize NativeScriptComponent with field deserialization.
    /// </summary>
    protected virtual void DeserializeNativeScriptComponent(Entity entity, JsonObject componentObj)
    {
        if (!componentObj.ContainsKey(ScriptTypeKey))
        {
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
        var scriptInstanceResult = ScriptEngine.Instance.CreateScriptInstance(scriptTypeName);
        if (scriptInstanceResult.IsSuccess)
        {
            var scriptInstance = scriptInstanceResult.Value;
            
            // Deserialize public fields/properties
            if (componentObj[FieldsKey] is JsonObject fieldsObj)
            {
                foreach (var (fieldName, fieldValueNode) in fieldsObj)
                {
                    try
                    {
                        if (fieldValueNode == null) continue;
                        
                        var exposed = scriptInstance
                            .GetExposedFields()
                            .AsValueEnumerable()
                            .FirstOrDefault(f => f.Name == fieldName);
                            
                        if (exposed.Name == null)
                        {
                            Logger.Warning("Script field '{FieldName}' not found in {ScriptType}", 
                                fieldName, scriptTypeName);
                            continue;
                        }
                        
                        var value = fieldValueNode.Deserialize(exposed.Type, SerializationConfig.DefaultOptions);
                        scriptInstance.SetFieldValue(fieldName, value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "Failed to deserialize script field '{FieldName}'", fieldName);
                    }
                }
            }
            
            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
            {
                ScriptableEntity = scriptInstance
            });
            return;
        }
        
        // If ScriptEngine fails, try to create built-in script types
        ScriptableEntity? builtInScript = scriptTypeName switch
        {
            nameof(CameraController) => new CameraController(),
            _ => null
        };

        if (builtInScript != null)
        {
            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
            {
                ScriptableEntity = builtInScript
            });
        }
        else
        {
            entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent());
            Logger.Warning("Failed to create script instance for type: {ScriptType}", scriptTypeName);
        }
    }
    
    /// <summary>
    /// Add a component to an entity from JSON.
    /// </summary>
    protected static void AddComponent<T>(Entity entity, JsonObject componentObj) where T : class, IComponent
    {
        var component = componentObj.Deserialize<T>(SerializationConfig.DefaultOptions);
        if (component != null)
        {
            entity.AddComponent<T>(component);
        }
    }
    
    /// <summary>
    /// Get a JSON array from a JSON object.
    /// </summary>
    protected static JsonArray GetJsonArray(JsonNode jsonObject, string key)
    {
        if (!jsonObject.AsObject().ContainsKey(key))
            throw new InvalidOperationException($"Missing required '{key}' key in JSON");
        
        return jsonObject[key] as JsonArray ?? 
               throw new InvalidOperationException($"'{key}' must be a JSON array");
    }
}
