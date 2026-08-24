using System.Text.Json;
using System.Text.Json.Nodes;
using ECS;
using SceneComponents;
using SceneComponents.Lighting;

namespace Engine.Scene.Serializer;

internal interface IComponentSerializer
{
    string ComponentName { get; }

    Type ComponentType { get; }

    bool TrySerialize(IComponent component, JsonSerializerOptions options, out JsonObject? componentJson);

    bool TryDeserialize(Entity entity, JsonObject componentJson, JsonSerializerOptions options);
}

internal sealed class JsonComponentSerializer<T> : IComponentSerializer where T : class, IComponent
{
    public JsonComponentSerializer(string? componentName = null) =>
        ComponentName = componentName ?? typeof(T).Name;

    public string ComponentName { get; }

    public Type ComponentType => typeof(T);

    public bool TrySerialize(IComponent component, JsonSerializerOptions options, out JsonObject? componentJson)
    {
        var node = JsonSerializer.SerializeToNode(component, ComponentType, options);
        if (node is not JsonObject obj)
        {
            componentJson = null;
            return false;
        }

        obj["Name"] = ComponentName;
        componentJson = obj;
        return true;
    }

    public bool TryDeserialize(Entity entity, JsonObject componentJson, JsonSerializerOptions options)
    {
        var component = componentJson.Deserialize<T>(options);
        if (component is null)
            return false;

        if (component is DirectionalLightComponent directionalLight)
            ApplyDirectionalLightJsonDefaults(directionalLight, componentJson);

        entity.AddComponent(component);
        return true;
    }

    private static void ApplyDirectionalLightJsonDefaults(
        DirectionalLightComponent component,
        JsonObject componentJson)
    {
        if (componentJson.ContainsKey(nameof(DirectionalLightComponent.Intensity)))
            return;

        if (componentJson.TryGetPropertyValue("Strength", out var strengthNode) && strengthNode is JsonValue)
            component.Intensity = strengthNode.GetValue<float>();
        else
            component.Intensity = 1.0f;
    }
}

internal sealed class NativeScriptComponentSerializer : IComponentSerializer
{
    private const string ScriptTypeKey = "ScriptType";

    public string ComponentName => nameof(NativeScriptComponent);

    public Type ComponentType => typeof(NativeScriptComponent);

    public bool TrySerialize(IComponent component, JsonSerializerOptions options, out JsonObject? componentJson)
    {
        var script = (NativeScriptComponent)component;
        var obj = new JsonObject { ["Name"] = ComponentName };

        if (!string.IsNullOrEmpty(script.ScriptTypeName))
            obj[ScriptTypeKey] = script.ScriptTypeName;

        componentJson = obj;
        return true;
    }

    public bool TryDeserialize(Entity entity, JsonObject componentJson, JsonSerializerOptions options)
    {
        var component = new NativeScriptComponent();
        var scriptTypeName = componentJson[ScriptTypeKey]?.GetValue<string>();
        if (!string.IsNullOrEmpty(scriptTypeName))
            component.ScriptTypeName = scriptTypeName;

        entity.AddComponent(component);
        return true;
    }
}
