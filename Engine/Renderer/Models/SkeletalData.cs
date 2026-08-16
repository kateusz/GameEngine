using System.Numerics;

namespace Engine.Renderer.Models;

public static class SkeletalLimits
{
    public const int MaxBones = 100;
}

public readonly record struct SkeletonBone(string Name, int ParentIndex, Matrix4x4 InverseBind);

public readonly record struct VectorKey(float Time, Vector3 Value);

public readonly record struct RotationKey(float Time, Quaternion Value);

public sealed record BoneChannel(
    int BoneIndex,
    IReadOnlyList<VectorKey> Positions,
    IReadOnlyList<RotationKey> Rotations,
    IReadOnlyList<VectorKey> Scales);

public sealed record AnimationClip(
    string Name,
    float Duration,
    IReadOnlyList<BoneChannel> Channels);
