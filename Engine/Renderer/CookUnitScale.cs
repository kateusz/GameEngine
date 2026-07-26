using System.Numerics;
using Engine.Renderer;

namespace Engine.Renderer;

/// <summary>Downscale oversized Assimp cooks (cm) to engine meters.</summary>
internal static class CookUnitScale
{
    public const float CmToMeters = 0.01f;
    public const float OversizedExtentThreshold = 20f;
    /// <summary>Anim translation >> mesh extent usually means mesh was converted to meters but keys were not.</summary>
    public const float AnimMeshExtentRatioThreshold = 5f;

    public readonly record struct ScaleFactors(float Mesh, float Anim);

    public static float DetectDownscaleFactor(IEnumerable<ModelSubmesh> submeshes, Anim3dAsset? animations = null) =>
        DetectScaleFactors(submeshes, animations).Mesh;

    public static ScaleFactors DetectScaleFactors(IEnumerable<ModelSubmesh> submeshes, Anim3dAsset? animations = null)
    {
        var extent = ComputeMaxExtent(submeshes);
        if (extent > OversizedExtentThreshold)
            return new(CmToMeters, CmToMeters);

        if (animations is not null && extent > 1e-3f)
        {
            var maxAnimTranslation = ComputeMaxTranslationMagnitude(animations);
            if (maxAnimTranslation / extent > AnimMeshExtentRatioThreshold)
                return new(1f, CmToMeters);
        }

        return new(1f, 1f);
    }

    public static void ApplyToSubmeshes(IEnumerable<ModelSubmesh> submeshes, float factor)
    {
        if (MathF.Abs(factor - 1f) < 1e-6f)
            return;

        foreach (var submesh in submeshes)
        {
            var mesh = submesh.Mesh;
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                var v = mesh.Vertices[i];
                mesh.Vertices[i] = v with { Position = v.Position * factor };
            }
        }
    }

    public static SkeletonAsset ApplyToSkeleton(SkeletonAsset skeleton, float factor)
    {
        if (MathF.Abs(factor - 1f) < 1e-6f)
            return skeleton;

        // Row-vector: v_new = v_old * S → IB_new = S^-1 * IB_old so v_new * IB_new = v_old * IB_old.
        var invScale = Matrix4x4.CreateScale(1f / factor);
        var bones = new List<SkeletonBone>(skeleton.Bones.Count);
        foreach (var bone in skeleton.Bones)
            bones.Add(bone with { InverseBind = invScale * bone.InverseBind });

        return new SkeletonAsset(bones);
    }

    /// <summary>
    /// Assimp FBX can leave offset matrices in cm while mesh verts are already meters.
    /// When IB translation dwarfs mesh extent, downscale InverseBind to mesh units.
    /// </summary>
    public static SkeletonAsset HarmonizeInverseBindWithMesh(
        SkeletonAsset skeleton,
        IEnumerable<ModelSubmesh> submeshes)
    {
        var extent = ComputeMaxExtent(submeshes);
        if (extent < 1e-3f)
            return skeleton;

        var maxIbT = 0f;
        foreach (var bone in skeleton.Bones)
        {
            var t = new Vector3(bone.InverseBind.M41, bone.InverseBind.M42, bone.InverseBind.M43);
            maxIbT = MathF.Max(maxIbT, t.Length());
        }

        if (maxIbT / extent <= AnimMeshExtentRatioThreshold)
            return skeleton;

        // Mesh is meters, Assimp offsets are cm: Offset_m = Scale(cm→m) * Offset_cm (column)
        // → IB_m = IB_cm * Scale(cm→m) (row / transposed).
        var s = Matrix4x4.CreateScale(CmToMeters);
        var bones = new List<SkeletonBone>(skeleton.Bones.Count);
        foreach (var bone in skeleton.Bones)
            bones.Add(bone with { InverseBind = bone.InverseBind * s });
        return new SkeletonAsset(bones);
    }

    public static Anim3dAsset ApplyToAnimations(Anim3dAsset animations, float factor)
    {
        if (MathF.Abs(factor - 1f) < 1e-6f)
            return animations;

        var clips = new List<Anim3dClip>(animations.Clips.Count);
        foreach (var clip in animations.Clips)
        {
            var channels = new List<Anim3dChannel>(clip.Channels.Count);
            foreach (var channel in clip.Channels)
            {
                var translations = channel.TranslationKeys
                    .Select(k => new Anim3dVec3Key(k.Time, k.Value * factor))
                    .ToList();
                channels.Add(new Anim3dChannel(
                    channel.BoneIndex, translations, channel.RotationKeys, channel.ScaleKeys));
            }

            clips.Add(new Anim3dClip(clip.Name, clip.DurationSeconds, channels));
        }

        return new Anim3dAsset(clips);
    }

    private static float ComputeMaxExtent(IEnumerable<ModelSubmesh> submeshes)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        var any = false;

        foreach (var submesh in submeshes)
        {
            foreach (var v in submesh.Mesh.Vertices)
            {
                any = true;
                min = Vector3.Min(min, v.Position);
                max = Vector3.Max(max, v.Position);
            }
        }

        if (!any)
            return 0f;

        var extent = max - min;
        var maxAbs = MathF.Max(MathF.Abs(min.X), MathF.Abs(max.X));
        maxAbs = MathF.Max(maxAbs, MathF.Abs(min.Y));
        maxAbs = MathF.Max(maxAbs, MathF.Abs(max.Y));
        maxAbs = MathF.Max(maxAbs, MathF.Abs(min.Z));
        maxAbs = MathF.Max(maxAbs, MathF.Abs(max.Z));
        return MathF.Max(MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)), maxAbs);
    }

    private static float ComputeMaxTranslationMagnitude(Anim3dAsset animations)
    {
        var max = 0f;
        foreach (var clip in animations.Clips)
        {
            foreach (var channel in clip.Channels)
            {
                foreach (var key in channel.TranslationKeys)
                    max = MathF.Max(max, key.Value.Length());
            }
        }

        return max;
    }
}
