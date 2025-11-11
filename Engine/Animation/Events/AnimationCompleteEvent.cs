using ECS;

namespace Engine.Animation;

/// <summary>
/// Event dispatched when non-looping animation reaches last frame and stops.
/// </summary>
public class AnimationCompleteEvent : IAnimationEvent
{
    /// <summary>
    /// Entity that completed the animation.
    /// </summary>
    public Entity Entity { get; init; }

    /// <summary>
    /// Name of the completed clip.
    /// </summary>
    public string ClipName { get; init; } = string.Empty;

    public AnimationCompleteEvent(Entity entity, string clipName)
    {
        Entity = entity;
        ClipName = clipName;
    }
}
