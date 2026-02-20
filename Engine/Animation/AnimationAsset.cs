using System.Numerics;
using System.Text.Json.Serialization;
using Engine.Renderer.Textures;

namespace Engine.Animation;

/// <summary>
/// Contains texture atlas reference and all animation clips.
/// </summary>
public sealed record AnimationAsset : IDisposable
{
    public required string Id { get; init; }

    /// <summary>
    /// Relative path to texture atlas file.
    /// </summary>
    public required string AtlasPath { get; init; }

    /// <summary>
    /// Loaded texture atlas reference.
    /// </summary>
    [JsonIgnore]
    public Texture2D Atlas { get; set; } = null!;
    
    public required Vector2 CellSize { get; init; }
    public required AnimationClip[] Clips { get; init; }
    public bool HasClip(string clipName) => Clips.Any(x => x.Name == clipName);
    public AnimationClip? GetClip(string clipName) => Clips.FirstOrDefault(c => c.Name == clipName);
    
    public void Dispose()
    {
        // Factory owns texture lifetime; just release our reference
        Atlas = null!;
    }
}