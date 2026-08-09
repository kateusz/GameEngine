using System.Numerics;

namespace Engine.Renderer.Skeletal;

/// <summary>CPU animation clip set DTO for *.anim3d (AN3D v1 / W4).</summary>
public sealed class Anim3dAsset
{
    public Anim3dAsset(IReadOnlyList<Anim3dClip> clips) => Clips = clips;

    public IReadOnlyList<Anim3dClip> Clips { get; }
}

public sealed record Anim3dClip(
    string Name,
    float DurationSeconds,
    IReadOnlyList<Anim3dChannel> Channels);

public sealed record Anim3dChannel(
    uint BoneIndex,
    IReadOnlyList<Anim3dVec3Key> TranslationKeys,
    IReadOnlyList<Anim3dQuatKey> RotationKeys,
    IReadOnlyList<Anim3dVec3Key> ScaleKeys);

public sealed record Anim3dVec3Key(float Time, Vector3 Value);

public sealed record Anim3dQuatKey(float Time, Quaternion Value);
