using System.Numerics;

namespace Engine.Renderer.Animation;

public readonly record struct VectorKey(float Time, Vector3 Value);
public readonly record struct QuatKey(float Time, Quaternion Value);

public sealed class BoneTrack
{
    public required int BoneIndex { get; init; }
    public required VectorKey[] Positions { get; init; }
    public required QuatKey[] Rotations { get; init; }
    public required VectorKey[] Scales { get; init; }
}

public sealed class AnimationClip
{
    public required string Name { get; init; }
    public required float DurationSeconds { get; init; }
    public required IReadOnlyList<BoneTrack> Tracks { get; init; }
}
