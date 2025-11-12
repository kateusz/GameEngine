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

    private readonly EventBus _eventBus;
    private readonly AnimationAssetManager _animationAssetManager;
    private readonly IContext _context;

    public AnimationSystem(EventBus eventBus, AnimationAssetManager animationAssetManager, IContext context)
    {
        _eventBus = eventBus;
        _animationAssetManager = animationAssetManager;
        _context = context;
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
        if (string.IsNullOrEmpty(animComponent.CurrentClipName) && animComponent.Asset.Clips.Length > 0)
        {
            animComponent.CurrentClipName = animComponent.Asset.Clips.First().Name;
        }

        // Validate clip exists
        if (!string.IsNullOrEmpty(animComponent.CurrentClipName) && !animComponent.Asset.HasClip(animComponent.CurrentClipName))
        {
            Logger.Warning("Animation clip not found: {CurrentClipName} in asset {AssetPath}", animComponent.CurrentClipName, animComponent.AssetPath);
            animComponent.CurrentClipName = animComponent.Asset.Clips.FirstOrDefault()?.Name ?? string.Empty;
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
        // Lazy-load asset for dynamically added components
        if (animComponent.Asset == null)
        {
            LoadAssetIfNeeded(animComponent);
            if (animComponent.Asset == null)
                return; // Asset failed to load
        }

        // Skip if not playing
        if (!animComponent.IsPlaying)
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

        // Use pre-calculated texture coordinates from animation frame
        // These coordinates are calculated during asset load and account for:
        renderer.TexCoords = currentFrame.TexCoords;

        // Note: Per-frame rotation and scale will be handled in a future phase
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
    public void OnInit()
    {
        Logger.Debug("AnimationSystem initialized with priority {Priority}", Priority);

        // Initialize animation assets for all existing entities with AnimationComponent
        var view = _context.View<AnimationComponent>();
        var entityCount = 0;
        foreach (var (_, animComponent) in view)
        {
            LoadAssetIfNeeded(animComponent);
            entityCount++;
        }

        Logger.Debug("Initialized {Count} animation components", entityCount);
    }

    public void OnShutdown()
    {
        Logger.Debug("Animationsystem shut down");
    }
}