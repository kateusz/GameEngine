using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.Animation;

/// <summary>
/// In-memory representation of a loaded animation asset.
/// Contains texture atlas reference and all animation clips.
/// </summary>
public class AnimationAsset : IDisposable
{
    /// <summary>
    /// Asset identifier (from JSON "id" field).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Relative path to texture atlas file.
    /// </summary>
    public required string AtlasPath { get; init; }

    /// <summary>
    /// Loaded texture atlas reference.
    /// </summary>
    public Texture2D? Atlas { get; set; }

    /// <summary>
    /// Default cell size [width, height] in pixels.
    /// Used for grid-based spritesheet layouts.
    /// </summary>
    public required Vector2 CellSize { get; init; }

    /// <summary>
    /// Dictionary of animation clips: clip name → clip data.
    /// </summary>
    public required AnimationClip[] Clips { get; init; }

    private Dictionary<string, AnimationClip>? _clipLookup;

    /// <summary>
    /// Initialize clip lookup dictionary for O(1) access.
    /// Called after deserialization.
    /// </summary>
    public void InitializeClipLookup()
    {
        _clipLookup = Clips.ToDictionary(c => c.Name);
    }

    /// <summary>
    /// Check if asset contains a clip with the given name.
    /// </summary>
    public bool HasClip(string clipName) => _clipLookup?.ContainsKey(clipName) ?? false;

    /// <summary>
    /// Get clip by name, returns null if not found.
    /// </summary>
    public AnimationClip? GetClip(string clipName) => _clipLookup?.GetValueOrDefault(clipName);

    /// <summary>
    /// Dispose texture resources.
    /// </summary>
    public void Dispose()
    {
        Atlas?.Dispose();
        Atlas = null;
    }
}