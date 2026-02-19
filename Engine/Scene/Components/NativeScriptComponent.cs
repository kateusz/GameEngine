using System.Text.Json.Serialization;
using ECS;

namespace Engine.Scene.Components;

public class NativeScriptComponent : IComponent
{
    [JsonIgnore]
    public ScriptableEntity? ScriptableEntity { get; set; }

    public IComponent Clone()
    {
        // Do not clone ScriptableEntity as it's runtime state
        // The script will be instantiated at runtime
        return new NativeScriptComponent
        {
            ScriptableEntity = null
        };
    }
}

