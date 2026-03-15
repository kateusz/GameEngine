using System.Text.Json.Serialization;
using ECS;

namespace Engine.Scene.Components;

public class NativeScriptComponent : IComponent
{
    /// <summary>
    /// Persisted script type name used to instantiate the script at runtime.
    /// Null when no script is assigned.
    /// </summary>
    public string? ScriptTypeName { get; set; }

    [JsonIgnore]
    public ScriptableEntity? ScriptableEntity { get; set; }

    public IComponent Clone()
    {
        // Do not clone ScriptableEntity as it's runtime state
        // The script will be instantiated at runtime
        return new NativeScriptComponent
        {
            ScriptTypeName = ScriptTypeName,
            ScriptableEntity = null
        };
    }
}

