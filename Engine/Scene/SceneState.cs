namespace Engine.Scene;

/// <summary>
/// Represents the current state of a scene in the editor or runtime.
/// </summary>
public enum SceneState
{
    /// <summary>
    /// Scene is in edit mode - no physics or scripts running.
    /// This is the default state in the editor.
    /// </summary>
    Edit = 0,

    /// <summary>
    /// Scene is playing - physics and scripts are updating normally.
    /// Time scale is 1.0 and all systems are active.
    /// </summary>
    Play = 1,

    /// <summary>
    /// Scene is paused - physics and scripts are frozen, rendering continues.
    /// Time scale is 0.0, allowing inspection of runtime state without modification.
    /// </summary>
    Paused = 2
}