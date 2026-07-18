using System.Numerics;

namespace Engine.Renderer.Animation;

public readonly record struct BoneData(string Name, int ParentIndex, Matrix4x4 InverseBind);

public sealed class Skeleton
{
    // Keep well under GL 3.3 min MAX_VERTEX_UNIFORM_COMPONENTS (1024).
    // lightingShader also needs view/model/normal matrices and scalars.
    public const int MaxBones = 32;

    public Skeleton(IReadOnlyList<BoneData> bones)
    {
        Bones = bones;
        RootBoneIndex = FindRootIndex(bones);
    }

    public IReadOnlyList<BoneData> Bones { get; }
    public int BoneCount => Bones.Count;
    public int RootBoneIndex { get; }
    public bool IsEmpty => Bones.Count == 0;

    private static int FindRootIndex(IReadOnlyList<BoneData> bones)
    {
        for (var i = 0; i < bones.Count; i++)
        {
            if (bones[i].ParentIndex < 0)
                return i;
        }

        return bones.Count > 0 ? 0 : -1;
    }
}
