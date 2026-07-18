using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public class AnimatorComponent : IComponent
{
    public string? ClipName { get; set; }
    public float Time { get; set; }
    public bool IsPlaying { get; set; }
    public bool Loop { get; set; } = true;
    public float Speed { get; set; } = 1f;
    public bool ApplyRootMotion { get; set; }

    /// <summary>Runtime pose for GPU skinning — not serialized.</summary>
    [JsonIgnore]
    public Matrix4x4[]? SkinMatrices { get; set; }

    [JsonIgnore]
    public bool HasPose { get; set; }

    [JsonIgnore]
    public Matrix4x4 PreviousRootGlobal { get; set; } = Matrix4x4.Identity;

    [JsonIgnore]
    public bool HasPreviousRoot { get; set; }

    public void Play(string clipName)
    {
        ClipName = clipName;
        Time = 0f;
        IsPlaying = true;
        HasPreviousRoot = false;
    }

    public void Stop()
    {
        IsPlaying = false;
        Time = 0f;
        HasPreviousRoot = false;
        HasPose = false;
        SkinMatrices = null;
    }

    public void Pause() => IsPlaying = false;

    public void Resume() => IsPlaying = true;

    public IComponent Clone() => new AnimatorComponent
    {
        ClipName = ClipName,
        Time = Time,
        IsPlaying = IsPlaying,
        Loop = Loop,
        Speed = Speed,
        ApplyRootMotion = ApplyRootMotion
    };
}
