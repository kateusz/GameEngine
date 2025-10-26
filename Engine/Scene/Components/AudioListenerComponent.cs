using ECS;

namespace Engine.Scene.Components;

/// <summary>
/// Component that represents an audio listener in the scene.
/// Typically attached to the main camera entity to hear audio from the player's perspective.
/// Only one audio listener should be active per scene.
/// </summary>
public class AudioListenerComponent : IComponent
{
    /// <summary>
    /// Whether this audio listener is currently active.
    /// Only one listener should be active at a time.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public AudioListenerComponent()
    {
    }

    public AudioListenerComponent(bool isActive)
    {
        IsActive = isActive;
    }

    public IComponent Clone()
    {
        return new AudioListenerComponent(IsActive);
    }
}
