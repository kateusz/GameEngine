using ECS;

namespace SceneComponents;

public class NativeScriptComponent : IComponent
{
    /// <summary>
    /// Persisted script type name used to instantiate the script at runtime.
    /// Null when no script is assigned.
    /// </summary>
    public string? ScriptTypeName { get; set; }

    public IComponent Clone()
    {
        return new NativeScriptComponent
        {
            ScriptTypeName = ScriptTypeName
        };
    }
}

