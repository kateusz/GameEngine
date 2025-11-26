using ECS;

namespace Engine.Animation.Events;

/// <summary>
/// Base interface for animation events.
/// </summary>
public interface IAnimationEvent
{
    /// <summary>
    /// Entity that triggered the event.
    /// </summary>
    Entity Entity { get; }

    /// <summary>
    /// Name of the animation clip.
    /// </summary>
    string ClipName { get; }
}