using System.Text.Json.Serialization;

namespace Engine.Animation;

/// <summary>
/// Represents a named animation clip
/// </summary>
public record AnimationClip
{
    /// <summary>
    /// Clip name (e.g., "idle", "walk")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Frames per second for this animation.
    /// </summary>
    public required float Fps { get; init; }

    /// <summary>
    /// Whether animation loops back to frame 0 after last frame.
    /// </summary>
    public required bool Loop { get; init; }

    /// <summary>
    /// Array of frame definitions.
    /// </summary>
    public required AnimationFrame[] Frames { get; init; } = [];

    /// <summary>
    /// Calculated clip duration in seconds.
    /// Formula: frames.Length / FPS
    /// </summary>
    [JsonIgnore]
    public float Duration => Frames.Length / Fps;

    /// <summary>
    /// Get frame duration in seconds (time per frame).
    /// </summary>
    [JsonIgnore]
    public float FrameDuration => 1.0f / Fps;
}