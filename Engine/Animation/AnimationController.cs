using ECS;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Animation;

/// <summary>
/// Helper class for controlling animation playback from scripts.
/// Provides a clean API for manipulating AnimationComponent data.
/// </summary>
public static class AnimationController
{
    private static readonly ILogger Logger = Log.ForContext(typeof(AnimationController));

    // ===== Basic Controls =====

    /// <summary>
    /// Play animation clip by name.
    /// </summary>
    /// <param name="entity">Entity with AnimationComponent</param>
    /// <param name="clipName">Name of clip to play</param>
    /// <param name="forceRestart">If true, restarts clip even if already playing</param>
    public static void Play(Entity entity, string clipName, bool forceRestart = false)
    {
        if (!entity.HasComponent<AnimationComponent>())
        {
            Logger.Warning("Entity {EntityName} does not have AnimationComponent", entity.Name);
            return;
        }

        var anim = entity.GetComponent<AnimationComponent>();

        if (anim.Asset == null || !anim.Asset.HasClip(clipName))
        {
            Logger.Warning("Animation clip not found: {ClipName} on entity {EntityName}", clipName, entity.Name);
            return;
        }

        // If already playing same clip and not forcing restart, do nothing
        if (anim.CurrentClipName == clipName && anim.IsPlaying && !forceRestart)
            return;

        anim.CurrentClipName = clipName;
        anim.CurrentFrameIndex = 0;
        anim.FrameTimer = 0.0f;
        anim.PreviousFrameIndex = -1;
        anim.IsPlaying = true;

        // Set loop from clip default
        var clip = anim.Asset.GetClip(clipName);
        if (clip != null)
            anim.Loop = clip.Loop;
    }

    /// <summary>
    /// Stop animation and reset to frame 0.
    /// </summary>
    public static void Stop(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        anim.IsPlaying = false;
        anim.CurrentFrameIndex = 0;
        anim.FrameTimer = 0.0f;
        anim.PreviousFrameIndex = -1;
    }

    /// <summary>
    /// Pause animation without resetting.
    /// </summary>
    public static void Pause(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        anim.IsPlaying = false;
    }

    /// <summary>
    /// Resume paused animation.
    /// </summary>
    public static void Resume(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim.Asset != null && anim.Asset.HasClip(anim.CurrentClipName))
            anim.IsPlaying = true;
    }

    // ===== Advanced Playback =====

    /// <summary>
    /// Set playback speed multiplier.
    /// </summary>
    public static void SetSpeed(Entity entity, float speed)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        anim.PlaybackSpeed = speed < 0.0f ? 0.0f : speed;
    }

    /// <summary>
    /// Jump to specific frame index.
    /// </summary>
    public static void SetFrame(Entity entity, int frameIndex)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim.Asset == null) return;
        
        var clip = anim.Asset.GetClip(anim.CurrentClipName);
        if (clip == null) return;

        // Clamp frameIndex to valid range
        if (frameIndex < 0) frameIndex = 0;
        if (frameIndex >= clip.Frames.Length) frameIndex = clip.Frames.Length - 1;
        
        anim.CurrentFrameIndex = frameIndex;
        anim.FrameTimer = 0.0f;
    }

    /// <summary>
    /// Set playback position as normalized time (0..1 range).
    /// </summary>
    public static void SetNormalizedTime(Entity entity, float t)
    {
        if (!entity.HasComponent<AnimationComponent>()) return;
        
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim.Asset == null) return;
        
        var clip = anim.Asset.GetClip(anim.CurrentClipName);
        if (clip == null) return;

        // Clamp t to 0..1 range
        if (t < 0.0f) t = 0.0f;
        if (t > 1.0f) t = 1.0f;
        
        int targetFrame = (int)(t * (clip.Frames.Length - 1));
        SetFrame(entity, targetFrame);
    }

    // ===== State Queries =====

    /// <summary>
    /// Get current frame index.
    /// </summary>
    public static int GetCurrentFrame(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return 0;
        return entity.GetComponent<AnimationComponent>().CurrentFrameIndex;
    }

    /// <summary>
    /// Get total frame count in current clip.
    /// </summary>
    public static int GetFrameCount(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return 0;
        
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim.Asset == null) return 0;
        
        var clip = anim.Asset.GetClip(anim.CurrentClipName);
        return clip?.Frames.Length ?? 0;
    }

    /// <summary>
    /// Get playback position as normalized time (0..1).
    /// </summary>
    public static float GetNormalizedTime(Entity entity)
    {
        int frameCount = GetFrameCount(entity);
        if (frameCount == 0) return 0.0f;
        
        int currentFrame = GetCurrentFrame(entity);
        return currentFrame / (float)(frameCount - 1);
    }

    /// <summary>
    /// Get name of current clip.
    /// </summary>
    public static string GetCurrentClipName(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return string.Empty;
        return entity.GetComponent<AnimationComponent>().CurrentClipName;
    }

    /// <summary>
    /// Check if asset contains clip with given name.
    /// </summary>
    public static bool HasClip(Entity entity, string clipName)
    {
        if (!entity.HasComponent<AnimationComponent>()) return false;
        
        var anim = entity.GetComponent<AnimationComponent>();
        return anim.Asset?.HasClip(clipName) ?? false;
    }

    /// <summary>
    /// Check if animation is currently playing.
    /// </summary>
    public static bool IsPlaying(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return false;
        return entity.GetComponent<AnimationComponent>().IsPlaying;
    }

    // ===== Clip Management =====

    /// <summary>
    /// Get array of all available clip names.
    /// </summary>
    public static string[] GetAvailableClips(Entity entity)
    {
        if (!entity.HasComponent<AnimationComponent>()) return [];
        
        var anim = entity.GetComponent<AnimationComponent>();
        return anim.Asset?.Clips.Select(c => c.Name)?.ToArray() ?? [];
    }

    /// <summary>
    /// Get clip duration in seconds.
    /// </summary>
    public static float GetClipDuration(Entity entity, string clipName)
    {
        if (!entity.HasComponent<AnimationComponent>()) return 0.0f;
        
        var anim = entity.GetComponent<AnimationComponent>();
        if (anim.Asset == null) return 0.0f;
        
        var clip = anim.Asset.GetClip(clipName);
        return clip?.Duration ?? 0.0f;
    }
}

