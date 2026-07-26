using System.Numerics;
using Engine.Renderer;
using Engine.Scene;

namespace Engine.Tests.Renderer;

/// <summary>CPU row-vector skinning matching lightingShader.vert SkinPosition().</summary>
internal static class SkinnedVertexTestMath
{
    public static Vector3 SkinPosition(Mesh.Vertex v, Matrix4x4[] palette)
    {
        var idx = new[] { (int)(v.BoneIndex.X + 0.5f), (int)(v.BoneIndex.Y + 0.5f), (int)(v.BoneIndex.Z + 0.5f), (int)(v.BoneIndex.W + 0.5f) };
        var wgt = new[] { v.BoneWeight.X, v.BoneWeight.Y, v.BoneWeight.Z, v.BoneWeight.W };
        var weightSum = wgt[0] + wgt[1] + wgt[2] + wgt[3];
        if (weightSum < 1e-5f)
            return v.Position;

        var pos = Vector4.Zero;
        for (var i = 0; i < 4; i++)
        {
            if (wgt[i] <= 0f)
                continue;

            var bone = idx[i] >= 0 && idx[i] < palette.Length ? palette[idx[i]] : Matrix4x4.Identity;
            pos += Vector4.Transform(new Vector4(v.Position, 1f), bone) * wgt[i];
        }

        return new Vector3(pos.X, pos.Y, pos.Z);
    }

    public static Vector3 SkinPositionLegacyBlend(Mesh.Vertex v, Matrix4x4[] palette)
    {
        var idx = new[] { (int)(v.BoneIndex.X + 0.5f), (int)(v.BoneIndex.Y + 0.5f), (int)(v.BoneIndex.Z + 0.5f), (int)(v.BoneIndex.W + 0.5f) };
        var wgt = new[] { v.BoneWeight.X, v.BoneWeight.Y, v.BoneWeight.Z, v.BoneWeight.W };
        var skin = Matrix4x4.Identity;
        for (var i = 0; i < 4; i++)
        {
            if (wgt[i] <= 0f)
                continue;
            var bone = idx[i] >= 0 && idx[i] < palette.Length ? palette[idx[i]] : Matrix4x4.Identity;
            skin = BlendMatrix(skin, bone, wgt[i]);
        }

        var p4 = Vector4.Transform(new Vector4(v.Position, 1f), skin);
        return new Vector3(p4.X, p4.Y, p4.Z);
    }

    public static void AssertAffineSkinWeights(Mesh.Vertex v, Matrix4x4[] palette)
    {
        var idx = new[] { (int)(v.BoneIndex.X + 0.5f), (int)(v.BoneIndex.Y + 0.5f), (int)(v.BoneIndex.Z + 0.5f), (int)(v.BoneIndex.W + 0.5f) };
        var wgt = new[] { v.BoneWeight.X, v.BoneWeight.Y, v.BoneWeight.Z, v.BoneWeight.W };
        for (var i = 0; i < 4; i++)
        {
            if (wgt[i] <= 0f)
                continue;

            var bone = idx[i] >= 0 && idx[i] < palette.Length ? palette[idx[i]] : Matrix4x4.Identity;
            var p4 = Vector4.Transform(new Vector4(v.Position, 1f), bone);
            if (MathF.Abs(p4.W - 1f) > 0.01f)
                throw new InvalidOperationException($"bone {idx[i]} palette row is non-affine: w={p4.W}");
        }
    }

    private static Matrix4x4 BlendMatrix(Matrix4x4 acc, Matrix4x4 bone, float weight) =>
        new(
            acc.M11 + bone.M11 * weight, acc.M12 + bone.M12 * weight, acc.M13 + bone.M13 * weight, acc.M14 + bone.M14 * weight,
            acc.M21 + bone.M21 * weight, acc.M22 + bone.M22 * weight, acc.M23 + bone.M23 * weight, acc.M24 + bone.M24 * weight,
            acc.M31 + bone.M31 * weight, acc.M32 + bone.M32 * weight, acc.M33 + bone.M33 * weight, acc.M34 + bone.M34 * weight,
            acc.M41 + bone.M41 * weight, acc.M42 + bone.M42 * weight, acc.M43 + bone.M43 * weight, acc.M44 + bone.M44 * weight);
}
