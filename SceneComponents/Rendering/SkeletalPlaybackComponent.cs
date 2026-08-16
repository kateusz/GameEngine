using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public class SkeletalPlaybackComponent : IComponent
{
    public const int MaxBones = 100;

    public string? MeshPath { get; set; }
    public string? ClipName { get; set; }
    public float Time { get; set; }
    public float Speed { get; set; } = 1f;
    public bool Loop { get; set; } = true;
    public bool Playing { get; set; }

    [JsonIgnore]
    public Matrix4x4[] BonePalette { get; set; } = CreateIdentityPalette();

    public IComponent Clone() => new SkeletalPlaybackComponent
    {
        MeshPath = MeshPath,
        ClipName = ClipName,
        Time = Time,
        Speed = Speed,
        Loop = Loop,
        Playing = Playing
    };

    public static Matrix4x4[] CreateIdentityPalette()
    {
        var palette = new Matrix4x4[MaxBones];
        for (var i = 0; i < palette.Length; i++)
            palette[i] = Matrix4x4.Identity;
        return palette;
    }
}
