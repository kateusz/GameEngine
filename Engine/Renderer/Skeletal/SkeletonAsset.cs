using System.Numerics;

namespace Engine.Renderer.Skeletal;

/// <summary>CPU skeleton DTO for *.skel (SKEL v1).</summary>
public sealed class SkeletonAsset
{
    public SkeletonAsset(IReadOnlyList<SkeletonBone> bones) => Bones = bones;

    public IReadOnlyList<SkeletonBone> Bones { get; }
}

public sealed record SkeletonBone(string Name, int ParentIndex, Matrix4x4 InverseBind);
