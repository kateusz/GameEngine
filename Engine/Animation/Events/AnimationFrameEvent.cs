using ECS;

namespace Engine.Animation;

/// <summary>
/// Event dispatched when animation enters a new frame that has events defined.
/// </summary>
public class AnimationFrameEvent : IAnimationEvent
{
    /// <summary>
    /// Entity that triggered the event.
    /// </summary>
    public Entity Entity { get; init; }

    /// <summary>
    /// Name of the animation clip.
    /// </summary>
    public string ClipName { get; init; } = string.Empty;

    /// <summary>
    /// Name of the event (from JSON "events" array).
    /// Examples: "footstep", "hit", "camera_shake"
    /// </summary>
    public string EventName { get; init; } = string.Empty;

    /// <summary>
    /// Frame index that triggered the event (0-based).
    /// </summary>
    public int FrameIndex { get; init; }

    /// <summary>
    /// Full frame data for advanced usage.
    /// </summary>
    public AnimationFrame? Frame { get; init; }

    public AnimationFrameEvent(Entity entity, string clipName, string eventName, int frameIndex, AnimationFrame? frame = null)
    {
        Entity = entity;
        ClipName = clipName;
        EventName = eventName;
        FrameIndex = frameIndex;
        Frame = frame;
    }
}