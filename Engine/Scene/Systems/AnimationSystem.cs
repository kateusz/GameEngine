using System.Numerics;
using ECS;
using Engine.Animation;
using Engine.Events;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// Updates animation state for all entities with AnimationComponent.
/// Advances frame timers, detects frame changes, dispatches events,
/// and updates SubTextureRendererComponent with current frame data.
///
/// Priority: 198 (after scripts, before rendering)
/// </summary>
public class AnimationSystem : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<AnimationSystem>();
    
    public int Priority => 198;

    private readonly Context _context;
    private readonly EventBus _eventBus;
    private readonly AnimationAssetManager _animationAssetManager;

    public AnimationSystem(Context context, EventBus eventBus, AnimationAssetManager animationAssetManager)
    {
        _context = context;
        _eventBus = eventBus;
        _animationAssetManager = animationAssetManager;
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var dt = (float)deltaTime.TotalSeconds;

        // Iterate over all entities with AnimationComponent
        foreach (var (entity, animComponent) in _context.View<AnimationComponent>())
        {
            // Update animation
            UpdateAnimation(entity, animComponent, dt);
        }
    }

    /// <summary>
    /// Load animation asset if AssetPath is set but Asset is null.
    /// </summary>
    private void LoadAssetIfNeeded(AnimationComponent animComponent)
    {
        // Skip if asset already loaded or no path specified
        if (animComponent.Asset != null || string.IsNullOrEmpty(animComponent.AssetPath))
            return;

        // Load asset
        animComponent.Asset = _animationAssetManager.LoadAsset(animComponent.AssetPath);

        if (animComponent.Asset == null)
        {
            Logger.Error("Failed to load animation asset: {AssetPath}", animComponent.AssetPath);
            return;
        }

        // If no clip specified, use first available clip
        if (string.IsNullOrEmpty(animComponent.CurrentClipName) && animComponent.Asset.Clips.Count > 0)
        {
            animComponent.CurrentClipName = animComponent.Asset.Clips.Keys.First();
        }

        // Validate clip exists
        if (!string.IsNullOrEmpty(animComponent.CurrentClipName) && !animComponent.Asset.HasClip(animComponent.CurrentClipName))
        {
            Logger.Warning("Animation clip not found: {CurrentClipName} in asset {AssetPath}", animComponent.CurrentClipName, animComponent.AssetPath);
            animComponent.CurrentClipName = animComponent.Asset.Clips.Keys.FirstOrDefault() ?? string.Empty;
        }

        // Set loop from clip default
        if (!string.IsNullOrEmpty(animComponent.CurrentClipName))
        {
            var clip = animComponent.Asset.GetClip(animComponent.CurrentClipName);
            if (clip != null)
                animComponent.Loop = clip.Loop;
        }

        Logger.Information("Animation asset loaded: {AssetPath} (clip: {CurrentClipName})", animComponent.AssetPath, animComponent.CurrentClipName);
    }

    /// <summary>
    /// Update single animation component.
    /// </summary>
    private void UpdateAnimation(Entity entity, AnimationComponent animComponent, float deltaTime)
    {
        // Skip if not playing or no asset loaded
        if (!animComponent.IsPlaying || animComponent.Asset == null)
            return;

        // Get current clip
        var clip = animComponent.Asset.GetClip(animComponent.CurrentClipName);
        if (clip == null)
        {
            Logger.Warning("Animation clip not found: {AnimComponentCurrentClipName} on entity {EntityName}", animComponent.CurrentClipName, entity.Name);
            animComponent.IsPlaying = false;
            return;
        }

        // Skip if clip has no frames
        if (clip.Frames.Length == 0)
        {
            Logger.Warning("Animation clip has no frames: {AnimComponentCurrentClipName}", animComponent.CurrentClipName);
            animComponent.IsPlaying = false;
            return;
        }

        // Store previous frame for event detection
        animComponent.PreviousFrameIndex = animComponent.CurrentFrameIndex;

        // Calculate frame duration from FPS
        var frameDuration = clip.FrameDuration;

        // Advance frame timer
        animComponent.FrameTimer += deltaTime * animComponent.PlaybackSpeed;

        // Handle frame advancement (may advance multiple frames if very fast playback)
        while (animComponent.FrameTimer >= frameDuration)
        {
            // Reset timer (keep overflow for precise timing)
            animComponent.FrameTimer -= frameDuration;

            // Advance frame
            animComponent.CurrentFrameIndex++;

            // Handle end of animation
            if (animComponent.CurrentFrameIndex >= clip.Frames.Length)
            {
                if (animComponent.Loop)
                {
                    // Loop back to start
                    animComponent.CurrentFrameIndex = 0;
                }
                else
                {
                    // Clamp to last frame and stop
                    animComponent.CurrentFrameIndex = clip.Frames.Length - 1;
                    animComponent.IsPlaying = false;
                    animComponent.FrameTimer = 0.0f;

                    // Dispatch animation complete event
                    DispatchCompleteEvent(entity, clip.Name);

                    // Don't update renderer if stopped on last frame
                    break;
                }
            }

            // Dispatch frame events if frame changed
            if (animComponent.CurrentFrameIndex < clip.Frames.Length)
            {
                var currentFrame = clip.Frames[animComponent.CurrentFrameIndex];
                DispatchFrameEvents(entity, clip.Name, currentFrame, animComponent.CurrentFrameIndex);
            }
        }

        // Update SubTextureRendererComponent with current frame
        UpdateRendererComponent(entity, animComponent, clip);
    }

    /// <summary>
    /// Update SubTextureRendererComponent with current frame data.
    /// </summary>
    private static void UpdateRendererComponent(Entity entity, AnimationComponent animComponent, AnimationClip clip)
    {
        // Check if entity has SubTextureRendererComponent
        if (!entity.HasComponent<SubTextureRendererComponent>())
        {
            Logger.Warning("Entity {EntityName} has AnimationComponent but no SubTextureRendererComponent", entity.Name);
            return;
        }

        var renderer = entity.GetComponent<SubTextureRendererComponent>();
        var currentFrame = clip.Frames[animComponent.CurrentFrameIndex];
        var asset = animComponent.Asset!;

        // Set texture from asset
        renderer.Texture = asset.Atlas;

        // Calculate grid coordinates from pixel rect
        // SubTextureRendererComponent uses grid coordinates (cell-based)
        // We need to convert pixel rect to grid coords
        var gridX = currentFrame.Rect.X / asset.CellSize.X;
        var gridY = currentFrame.Rect.Y / asset.CellSize.Y;
        renderer.Coords = new Vector2(gridX, gridY);

        // Set cell size
        renderer.CellSize = asset.CellSize;

        // Calculate sprite size in cells
        var cellsX = currentFrame.Rect.Width / asset.CellSize.X;
        var cellsY = currentFrame.Rect.Height / asset.CellSize.Y;
        renderer.SpriteSize = new Vector2(cellsX, cellsY);

        // Note: Per-frame flip, rotation, scale handled in Phase 5
        // For now, we only update texture coordinates
    }

    /// <summary>
    /// Dispatch frame events for current frame.
    /// </summary>
    private void DispatchFrameEvents(Entity entity, string clipName, AnimationFrame frame, int frameIndex)
    {
        // Only dispatch if frame has events
        if (frame.Events.Length == 0)
            return;

        foreach (var eventName in frame.Events)
        {
            var evt = new AnimationFrameEvent(entity, clipName, eventName, frameIndex, frame);
            _eventBus.Publish(evt);

            Logger.Debug("Animation frame event: {EntityName}.{ClipName}[{FrameIndex}] → {EventName}", entity.Name, clipName, frameIndex, eventName);
        }
    }

    /// <summary>
    /// Dispatch animation complete event.
    /// </summary>
    private void DispatchCompleteEvent(Entity entity, string clipName)
    {
        var evt = new AnimationCompleteEvent(entity, clipName);
        _eventBus.Publish(evt);

        Logger.Debug("Animation complete: {EntityName}.{ClipName}", entity.Name, clipName);
    }

    // ISystem interface methods
    public void OnInit() { }
    public void OnShutdown() { }
}