using System.Numerics;

namespace Engine.Renderer.Animation;

/// <summary>
/// Samples a clip against a skeleton into skin matrices (model space) and optional root translation delta.
/// </summary>
internal static class PoseEvaluator
{
    public readonly record struct PoseResult(Matrix4x4[] SkinMatrices, Vector3 RootDelta, Matrix4x4 RootGlobal);

    public static PoseResult Evaluate(
        Skeleton skeleton,
        AnimationClip? clip,
        float timeSeconds,
        bool loop,
        bool computeRootDelta,
        Matrix4x4 previousRootGlobal,
        bool hasPreviousRoot)
    {
        var boneCount = skeleton.BoneCount;
        var locals = new Matrix4x4[boneCount];
        var globals = new Matrix4x4[boneCount];
        var skins = new Matrix4x4[boneCount];

        for (var i = 0; i < boneCount; i++)
            locals[i] = Matrix4x4.Identity;

        if (clip != null && clip.DurationSeconds > 0f && clip.Tracks.Count > 0)
        {
            var t = ResolveTime(timeSeconds, clip.DurationSeconds, loop);
            foreach (var track in clip.Tracks)
            {
                if ((uint)track.BoneIndex >= (uint)boneCount)
                    continue;
                locals[track.BoneIndex] = SampleLocal(track, t);
            }
        }

        for (var i = 0; i < boneCount; i++)
        {
            var parent = skeleton.Bones[i].ParentIndex;
            globals[i] = parent >= 0 && parent < boneCount
                ? locals[i] * globals[parent]
                : locals[i];
            // Row-vector skin: v' = v * offset * global (Assimp offset = inverse bind).
            skins[i] = skeleton.Bones[i].InverseBind * globals[i];
        }

        var rootDelta = Vector3.Zero;
        var rootGlobal = Matrix4x4.Identity;
        var rootIndex = skeleton.RootBoneIndex;
        if (rootIndex >= 0)
        {
            rootGlobal = globals[rootIndex];
            if (computeRootDelta && hasPreviousRoot)
            {
                var prev = previousRootGlobal.Translation;
                var curr = rootGlobal.Translation;
                rootDelta = curr - prev;
            }
        }

        return new PoseResult(skins, rootDelta, rootGlobal);
    }

    public static float ResolveTime(float timeSeconds, float durationSeconds, bool loop)
    {
        if (durationSeconds <= 0f)
            return 0f;

        if (loop)
        {
            var t = timeSeconds % durationSeconds;
            return t < 0f ? t + durationSeconds : t;
        }

        return System.Math.Clamp(timeSeconds, 0f, durationSeconds);
    }

    /// <summary>
    /// True when advancing from previousTime to timeSeconds crossed a loop boundary.
    /// </summary>
    public static bool CrossedLoopBoundary(float previousTime, float timeSeconds, float durationSeconds, bool loop)
    {
        if (!loop || durationSeconds <= 0f)
            return false;

        var prev = ResolveTime(previousTime, durationSeconds, loop);
        var curr = ResolveTime(timeSeconds, durationSeconds, loop);
        return timeSeconds > previousTime && curr < prev;
    }

    private static Matrix4x4 SampleLocal(BoneTrack track, float time)
    {
        var position = SampleVec(track.Positions, time, Vector3.Zero);
        var rotation = SampleQuat(track.Rotations, time, Quaternion.Identity);
        var scale = SampleVec(track.Scales, time, Vector3.One);
        return Matrix4x4.CreateScale(scale)
               * Matrix4x4.CreateFromQuaternion(rotation)
               * Matrix4x4.CreateTranslation(position);
    }

    private static Vector3 SampleVec(VectorKey[] keys, float time, Vector3 fallback)
    {
        if (keys.Length == 0)
            return fallback;
        if (keys.Length == 1)
            return keys[0].Value;

        if (time <= keys[0].Time)
            return keys[0].Value;
        if (time >= keys[^1].Time)
            return keys[^1].Value;

        for (var i = 0; i < keys.Length - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (time > b.Time)
                continue;

            var span = b.Time - a.Time;
            var alpha = span > 1e-8f ? (time - a.Time) / span : 0f;
            return Vector3.Lerp(a.Value, b.Value, alpha);
        }

        return keys[^1].Value;
    }

    private static Quaternion SampleQuat(QuatKey[] keys, float time, Quaternion fallback)
    {
        if (keys.Length == 0)
            return fallback;
        if (keys.Length == 1)
            return Quaternion.Normalize(keys[0].Value);

        if (time <= keys[0].Time)
            return Quaternion.Normalize(keys[0].Value);
        if (time >= keys[^1].Time)
            return Quaternion.Normalize(keys[^1].Value);

        for (var i = 0; i < keys.Length - 1; i++)
        {
            var a = keys[i];
            var b = keys[i + 1];
            if (time > b.Time)
                continue;

            var span = b.Time - a.Time;
            var alpha = span > 1e-8f ? (time - a.Time) / span : 0f;
            return Quaternion.Normalize(Quaternion.Slerp(a.Value, b.Value, alpha));
        }

        return Quaternion.Normalize(keys[^1].Value);
    }
}
