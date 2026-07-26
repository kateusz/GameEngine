using System.Numerics;
using Engine.Renderer;
using SceneComponents.Rendering;

namespace Engine.Scene;

/// <summary>
/// Pure skeletal pose evaluation (no ECS/GPU). Row-vector convention throughout (v' = v·M):
/// locals = S×R×T; globals = local × parentGlobal; palette = InverseBind × global(time).
/// Rest locals come from cooked inverse binds; channels are retargeted as deltas onto rest
/// so the first key frame skins as bind pose (Mixamo keys often disagree with Assimp IB).
/// </summary>
public static class SkeletalPoseMath
{
    public const int MaxBones = SkeletalPlaybackComponent.MaxBones;

    public static void Evaluate(SkeletonAsset skeleton, Anim3dClip clip, float timeSeconds, Matrix4x4[] destination)
    {
        ArgumentNullException.ThrowIfNull(skeleton);
        ArgumentNullException.ThrowIfNull(clip);
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < MaxBones)
            throw new ArgumentException($"destination must have length >= {MaxBones}", nameof(destination));

        var boneCount = skeleton.Bones.Count;
        if (boneCount > MaxBones)
            throw new InvalidOperationException($"Skeleton has {boneCount} bones; max is {MaxBones}");

        Span<Matrix4x4> locals = stackalloc Matrix4x4[boneCount];
        Span<Matrix4x4> globals = stackalloc Matrix4x4[boneCount];

        BuildRestLocals(skeleton, locals);
        ApplyChannels(skeleton, clip, timeSeconds, locals);
        BuildGlobals(skeleton, locals, globals);

        for (var i = 0; i < boneCount; i++)
            destination[i] = skeleton.Bones[i].InverseBind * globals[i];

        for (var i = boneCount; i < MaxBones; i++)
            destination[i] = Matrix4x4.Identity;
    }

    /// <summary>Earliest key time across a channel's tracks (0 if none).</summary>
    public static float ChannelBindTime(Anim3dChannel channel)
    {
        var t0 = float.MaxValue;
        if (channel.TranslationKeys.Count > 0)
            t0 = MathF.Min(t0, channel.TranslationKeys[0].Time);
        if (channel.RotationKeys.Count > 0)
            t0 = MathF.Min(t0, channel.RotationKeys[0].Time);
        if (channel.ScaleKeys.Count > 0)
            t0 = MathF.Min(t0, channel.ScaleKeys[0].Time);
        return t0 == float.MaxValue ? 0f : t0;
    }

    /// <summary>
    /// Local bone matrix from Assimp-style TRS keys.
    /// Assimp column locals are T×R×S (scale first); the row-vector equivalent is S×R×T,
    /// keeping the joint offset fixed in the parent frame while rotation acts about the joint.
    /// </summary>
    public static Matrix4x4 ComposeLocal(Vector3 translation, Quaternion rotation, Vector3 scale) =>
        Matrix4x4.CreateScale(scale)
        * Matrix4x4.CreateFromQuaternion(rotation)
        * Matrix4x4.CreateTranslation(translation);

    /// <summary>
    /// Bind-pose locals derived from InverseBind: bindGlobal = inv(IB),
    /// local = bindGlobal × inv(parentBindGlobal) (row-vector: local applies before parent).
    /// </summary>
    public static void BuildRestLocals(SkeletonAsset skeleton, Span<Matrix4x4> locals)
    {
        var boneCount = skeleton.Bones.Count;
        if (locals.Length < boneCount)
            throw new ArgumentException("locals span shorter than bone count", nameof(locals));

        Span<Matrix4x4> bindGlobals = stackalloc Matrix4x4[boneCount];
        for (var i = 0; i < boneCount; i++)
        {
            if (!Matrix4x4.Invert(skeleton.Bones[i].InverseBind, out bindGlobals[i]))
                bindGlobals[i] = Matrix4x4.Identity;
        }

        for (var i = 0; i < boneCount; i++)
        {
            var parent = skeleton.Bones[i].ParentIndex;
            if (parent < 0)
            {
                locals[i] = bindGlobals[i];
                continue;
            }

            if (!Matrix4x4.Invert(bindGlobals[parent], out var invParent))
                invParent = Matrix4x4.Identity;
            locals[i] = bindGlobals[i] * invParent;
        }
    }

    private static void ApplyChannels(
        SkeletonAsset skeleton,
        Anim3dClip clip,
        float timeSeconds,
        Span<Matrix4x4> locals)
    {
        var boneCount = skeleton.Bones.Count;
        foreach (var channel in clip.Channels)
        {
            if (channel.BoneIndex >= (uint)boneCount)
                continue;

            var boneIndex = (int)channel.BoneIndex;
            var restLocal = locals[boneIndex];
            Matrix4x4.Decompose(restLocal, out var restS, out var restR, out var restT);

            var t0 = ChannelBindTime(channel);
            var key0 = SampleChannelLocal(channel, t0, restT, restR, restS);
            var keyT = SampleChannelLocal(channel, timeSeconds, restT, restR, restS);

            // Retarget: apply motion delta from first keys onto skin-bind rest locals.
            // Row-vector: delta = keyT × inv(key0) acts in the bone's own frame, before rest —
            // rotation deltas pivot about the joint and the parent-frame offset stays rigid.
            // At t=t0: key0 × inv(key0) × rest = rest → IB×G = I.
            if (!Matrix4x4.Invert(key0, out var invKey0))
                invKey0 = Matrix4x4.Identity;

            locals[boneIndex] = keyT * invKey0 * restLocal;
        }
    }

    private static Matrix4x4 SampleChannelLocal(
        Anim3dChannel channel,
        float time,
        Vector3 restT,
        Quaternion restR,
        Vector3 restS)
    {
        var t = channel.TranslationKeys.Count > 0
            ? SampleVec3(channel.TranslationKeys, time, restT)
            : restT;
        var r = channel.RotationKeys.Count > 0
            ? SampleQuat(channel.RotationKeys, time, restR)
            : restR;
        var s = channel.ScaleKeys.Count > 0
            ? SampleVec3(channel.ScaleKeys, time, restS)
            : restS;
        return ComposeLocal(t, r, s);
    }

    private static void BuildGlobals(SkeletonAsset skeleton, ReadOnlySpan<Matrix4x4> locals, Span<Matrix4x4> globals)
    {
        var boneCount = skeleton.Bones.Count;
        for (var i = 0; i < boneCount; i++)
        {
            var parent = skeleton.Bones[i].ParentIndex;
            globals[i] = parent < 0
                ? locals[i]
                : locals[i] * globals[parent];
        }
    }

    private static Vector3 SampleVec3(IReadOnlyList<Anim3dVec3Key> keys, float time, Vector3 identity)
    {
        if (keys.Count == 0)
            return identity;
        if (keys.Count == 1 || time <= keys[0].Time)
            return keys[0].Value;
        if (time >= keys[^1].Time)
            return keys[^1].Value;

        for (var i = 0; i < keys.Count - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (time > b.Time)
                continue;

            var span = b.Time - a.Time;
            var t = span > 1e-8f ? (time - a.Time) / span : 0f;
            return Vector3.Lerp(a.Value, b.Value, t);
        }

        return keys[^1].Value;
    }

    private static Quaternion SampleQuat(IReadOnlyList<Anim3dQuatKey> keys, float time, Quaternion identity)
    {
        if (keys.Count == 0)
            return identity;
        if (keys.Count == 1 || time <= keys[0].Time)
            return SafeNormalize(keys[0].Value);
        if (time >= keys[^1].Time)
            return SafeNormalize(keys[^1].Value);

        for (var i = 0; i < keys.Count - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (time > b.Time)
                continue;

            var span = b.Time - a.Time;
            var t = span > 1e-8f ? (time - a.Time) / span : 0f;
            return SafeNormalize(Quaternion.Slerp(a.Value, b.Value, t));
        }

        return SafeNormalize(keys[^1].Value);
    }

    private static Quaternion SafeNormalize(Quaternion q)
    {
        if (!float.IsFinite(q.X) || !float.IsFinite(q.Y) || !float.IsFinite(q.Z) || !float.IsFinite(q.W))
            return Quaternion.Identity;

        var lenSq = q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W;
        return lenSq < 1e-12f ? Quaternion.Identity : Quaternion.Normalize(q);
    }
}
