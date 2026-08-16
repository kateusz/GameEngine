using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Models;

namespace Engine.Scene.Skeletal;

public static class SkeletalPoseMath
{
    public static Matrix4x4[] CreateIdentityPalette()
    {
        var palette = new Matrix4x4[SkeletalLimits.MaxBones];
        for (var i = 0; i < palette.Length; i++)
            palette[i] = Matrix4x4.Identity;
        return palette;
    }

    public static void FillIdentity(Matrix4x4[] palette)
    {
        for (var i = 0; i < palette.Length; i++)
            palette[i] = Matrix4x4.Identity;
    }

    public static float AdvanceTime(float time, float deltaSeconds, float speed, float duration, bool loop)
    {
        if (duration <= 0f)
            return 0f;

        time += deltaSeconds * speed;
        if (loop)
        {
            time %= duration;
            if (time < 0f)
                time += duration;
            return time;
        }

        return System.Math.Clamp(time, 0f, duration);
    }

    public static void Evaluate(
        IReadOnlyList<SkeletonBone> bones,
        AnimationClip? clip,
        float time,
        Matrix4x4[] palette)
    {
        FillIdentity(palette);
        if (bones.Count == 0)
            return;

        var locals = RestLocals(bones);
        ApplyChannels(bones.Count, clip, time, locals);
        var globals = new Matrix4x4[bones.Count];

        for (var i = 0; i < bones.Count; i++)
        {
            var parent = bones[i].ParentIndex;
            globals[i] = parent < 0 ? locals[i] : locals[i] * globals[parent];
            palette[i] = bones[i].InverseBind * globals[i];
        }
    }

    private static Matrix4x4[] RestLocals(IReadOnlyList<SkeletonBone> bones)
    {
        var bindGlobals = new Matrix4x4[bones.Count];
        var rest = new Matrix4x4[bones.Count];
        for (var i = 0; i < bones.Count; i++)
        {
            if (!Matrix4x4.Invert(bones[i].InverseBind, out bindGlobals[i]))
                bindGlobals[i] = Matrix4x4.Identity;
        }

        for (var i = 0; i < bones.Count; i++)
        {
            var parent = bones[i].ParentIndex;
            if (parent < 0)
            {
                rest[i] = bindGlobals[i];
                continue;
            }

            if (!Matrix4x4.Invert(bindGlobals[parent], out var invParent))
                invParent = Matrix4x4.Identity;
            rest[i] = bindGlobals[i] * invParent;
        }

        return rest;
    }

    // Clip channels replace bone-local TRS. Missing tracks keep inverse-bind rest.
    private static void ApplyChannels(int boneCount, AnimationClip? clip, float time, Matrix4x4[] locals)
    {
        if (clip is null)
            return;

        foreach (var channel in clip.Channels)
        {
            if ((uint)channel.BoneIndex >= (uint)boneCount)
                continue;

            var restLocal = locals[channel.BoneIndex];
            Matrix4x4.Decompose(restLocal, out var restS, out var restR, out var restT);
            locals[channel.BoneIndex] = SampleChannelLocal(channel, time, restT, restR, restS);
        }
    }

    private static Matrix4x4 SampleChannelLocal(
        BoneChannel channel, float time, Vector3 restT, Quaternion restR, Vector3 restS)
    {
        var t = channel.Positions.Count > 0 ? SampleVec(channel.Positions, time, restT) : restT;
        var r = channel.Rotations.Count > 0
            ? SafeNormalize(SampleRot(channel.Rotations, time))
            : restR;
        var s = channel.Scales.Count > 0 ? SampleVec(channel.Scales, time, restS) : restS;
        return Matrix4x4.CreateScale(s)
               * Matrix4x4.CreateFromQuaternion(r)
               * Matrix4x4.CreateTranslation(t);
    }

    private static Vector3 SampleVec(IReadOnlyList<VectorKey> keys, float time, Vector3 fallback)
    {
        if (keys.Count == 0)
            return fallback;
        if (keys.Count == 1 || time <= keys[0].Time)
            return keys[0].Value;
        if (time >= keys[^1].Time)
            return keys[^1].Value;

        for (var i = 0; i < keys.Count - 1; i++)
        {
            if (time >= keys[i + 1].Time)
                continue;
            var span = keys[i + 1].Time - keys[i].Time;
            var t = span <= 1e-8f ? 0f : (time - keys[i].Time) / span;
            return Vector3.Lerp(keys[i].Value, keys[i + 1].Value, t);
        }

        return keys[^1].Value;
    }

    private static Quaternion SampleRot(IReadOnlyList<RotationKey> keys, float time)
    {
        if (keys.Count == 0)
            return Quaternion.Identity;
        if (keys.Count == 1 || time <= keys[0].Time)
            return keys[0].Value;
        if (time >= keys[^1].Time)
            return keys[^1].Value;

        for (var i = 0; i < keys.Count - 1; i++)
        {
            if (time >= keys[i + 1].Time)
                continue;
            var span = keys[i + 1].Time - keys[i].Time;
            var t = span <= 1e-8f ? 0f : (time - keys[i].Time) / span;
            return Quaternion.Slerp(keys[i].Value, keys[i + 1].Value, t);
        }

        return keys[^1].Value;
    }

    private static Quaternion SafeNormalize(Quaternion q)
    {
        if (!float.IsFinite(q.X) || !float.IsFinite(q.Y) || !float.IsFinite(q.Z) || !float.IsFinite(q.W))
            return Quaternion.Identity;

        var lenSq = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
        return lenSq < 1e-12f ? Quaternion.Identity : Quaternion.Normalize(q);
    }
}
