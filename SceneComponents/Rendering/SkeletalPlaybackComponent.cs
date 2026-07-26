using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

/// <summary>
/// Data-only skeletal clip playback. Paths only; <see cref="BonePalette"/> is a transient pose handoff (W2).
/// </summary>
public class SkeletalPlaybackComponent : IComponent
{
    public const int MaxBones = 100;

    public string? SkeletonPath { get; set; }
    public string? ClipPath { get; set; }
    public string? ClipName { get; set; }
    public float Time { get; set; }
    public float Speed { get; set; } = 1f;
    public bool Loop { get; set; } = true;
    public bool Playing { get; set; }

    /// <summary>Per-frame bone palette written by SkeletalAnimationSystem; not serialized.</summary>
    [JsonIgnore]
    public Matrix4x4[] BonePalette { get; set; } = CreateIdentityPalette();

    public IComponent Clone() => new SkeletalPlaybackComponent
    {
        SkeletonPath = SkeletonPath,
        ClipPath = ClipPath,
        ClipName = ClipName,
        Time = Time,
        Speed = Speed,
        Loop = Loop,
        Playing = Playing
        // BonePalette stays identity on clone — runtime pose is not scene data
    };

    public static Matrix4x4[] CreateIdentityPalette()
    {
        var palette = new Matrix4x4[MaxBones];
        for (var i = 0; i < MaxBones; i++)
            palette[i] = Matrix4x4.Identity;
        return palette;
    }
}
