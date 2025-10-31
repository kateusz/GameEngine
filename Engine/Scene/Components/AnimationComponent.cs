using System.Text.Json.Serialization;
using ECS;
using Engine.Animation;

namespace Engine.Scene.Components;

/// <summary>
/// Data component for sprite animations using texture atlases.
/// Requires SubTextureRendererComponent for rendering.
/// All animation logic is handled by AnimationSystem.
/// </summary>
public class AnimationComponent : IComponent
{
    /// <summary>
    /// Reference to loaded animation asset.
    /// Managed by AnimationSystem.
    /// </summary>
    [JsonIgnore]
    public AnimationAsset? Asset { get; set; }

    /// <summary>
    /// Path to animation JSON file (relative to Assets/).
    /// Example: "Animations/Characters/player.json"
    /// </summary>
    public string? AssetPath { get; set; }

    /// <summary>
    /// Name of currently playing animation clip.
    /// </summary>
    public string CurrentClipName { get; set; } = string.Empty;

    /// <summary>
    /// Whether animation is currently playing.
    /// </summary>
    public bool IsPlaying { get; set; } = false;

    /// <summary>
    /// Whether to loop current animation.
    /// Can override asset's default loop setting.
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Playback speed multiplier.
    /// 1.0 = normal speed, 0.5 = half speed, 2.0 = double speed.
    /// </summary>
    public float PlaybackSpeed { get; set; } = 1.0f;

    /// <summary>
    /// Current frame index within clip (0-based).
    /// </summary>
    [JsonIgnore]
    public int CurrentFrameIndex = 0;

    /// <summary>
    /// Frame timing accumulator (0..frameDuration).
    /// </summary>
    [JsonIgnore]
    public float FrameTimer = 0.0f;

    /// <summary>
    /// Previous frame index (for event detection).
    /// </summary>
    [JsonIgnore]
    public int PreviousFrameIndex = -1;

    /// <summary>
    /// Whether to display debug overlay in viewport.
    /// </summary>
    public bool ShowDebugInfo { get; set; }
    

    public IComponent Clone()
    {
        return new AnimationComponent
        {
            AssetPath = AssetPath,
            CurrentClipName = CurrentClipName,
            IsPlaying = IsPlaying,
            Loop = Loop,
            PlaybackSpeed = PlaybackSpeed,
            ShowDebugInfo = ShowDebugInfo
            // Note: Runtime state (CurrentFrameIndex, FrameTimer) intentionally not cloned
        };
    }
}
