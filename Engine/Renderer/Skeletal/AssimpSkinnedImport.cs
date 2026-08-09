using Engine.Renderer;

namespace Engine.Renderer.Skeletal;

internal sealed class AssimpSkinnedImport(
    IReadOnlyList<AssimpModelPart> parts,
    SkeletonAsset skeleton,
    Anim3dAsset animations)
{
    public IReadOnlyList<AssimpModelPart> Parts { get; } = parts;
    public SkeletonAsset Skeleton { get; } = skeleton;
    public Anim3dAsset Animations { get; } = animations;
}
