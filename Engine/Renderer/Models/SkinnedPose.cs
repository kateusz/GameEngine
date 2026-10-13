using System.Numerics;
using ECS;
using Engine.Project;
using SceneComponents;
using SceneComponents.Rendering;

namespace Engine.Renderer.Models;

public static class SkinnedPose
{
    public static void Advance(Context context, IModelFactory models, TimeSpan delta)
    {
        var dt = (float)delta.TotalSeconds;
        if (!float.IsFinite(dt) || dt < 0f)
            dt = 0f;

        foreach (var (entity, playback) in context.View<SkinnedAnimationComponent>())
        {
            if (!entity.TryGetComponent<ModelRendererComponent>(out var renderer)
                || string.IsNullOrWhiteSpace(renderer.ModelPath))
            {
                playback.PoseReady = false;
                continue;
            }

            var model = models.Create(PathBuilder.Resolve(renderer.ModelPath));
            if (model?.Skin is not { } skin)
            {
                playback.PoseReady = false;
                continue;
            }

            playback.EnsureCapacity(skin.Joints.Length);
            if (!AdvanceClock(playback, skin, dt, out var clip))
            {
                playback.PoseReady = false;
                continue;
            }

            var visible = !entity.TryGetComponent<TransformComponent>(out var transform)
                          || transform.EffectiveVisible;
            if (!visible)
            {
                playback.PoseReady = false;
                continue;
            }

            SampleLocals(playback, skin, clip);
            BuildPalette(skin, playback.Locals, playback.Pose);
            playback.PoseReady = true;
        }
    }

    public static void BuildRest(SkinData skin, Matrix4x4[] destination)
    {
        var count = skin.Joints.Length;
        if (count > destination.Length)
            return;

        Span<Matrix4x4> locals = stackalloc Matrix4x4[count];
        for (var i = 0; i < count; i++)
            locals[i] = LocalMatrix(skin.Joints[i].RestTranslation, skin.Joints[i].RestRotation, skin.Joints[i].RestScale);
        BuildPalette(skin, locals, destination);
    }

    public static void WriteBlock(ReadOnlySpan<Matrix4x4> palette, Span<float> destination)
    {
        var count = System.Math.Min(palette.Length, destination.Length / 16);
        for (var i = 0; i < count; i++)
        {
            var matrix = Matrix4x4.Transpose(palette[i]);
            var offset = i * 16;
            destination[offset] = matrix.M11;
            destination[offset + 1] = matrix.M12;
            destination[offset + 2] = matrix.M13;
            destination[offset + 3] = matrix.M14;
            destination[offset + 4] = matrix.M21;
            destination[offset + 5] = matrix.M22;
            destination[offset + 6] = matrix.M23;
            destination[offset + 7] = matrix.M24;
            destination[offset + 8] = matrix.M31;
            destination[offset + 9] = matrix.M32;
            destination[offset + 10] = matrix.M33;
            destination[offset + 11] = matrix.M34;
            destination[offset + 12] = matrix.M41;
            destination[offset + 13] = matrix.M42;
            destination[offset + 14] = matrix.M43;
            destination[offset + 15] = matrix.M44;
        }
    }

    private static bool AdvanceClock(SkinnedAnimationComponent playback, SkinData skin, float dt, out ClipData clip)
    {
        clip = null!;
        ClipData? found = null;
        if (playback.ClipName.Length > 0)
        {
            foreach (var candidate in skin.Clips)
            {
                if (candidate.Name == playback.ClipName)
                {
                    found = candidate;
                    break;
                }
            }
        }

        if (found == null || found.Duration <= 0f || !float.IsFinite(found.Duration))
        {
            playback.Time = 0f;
            playback.LastTime = 0f;
            return false;
        }

        clip = found;

        var time = playback.Time;
        if (!float.IsFinite(time))
            time = 0f;
        time += dt;
        if (playback.Loop)
            time -= MathF.Floor(time / clip.Duration) * clip.Duration;
        else if (time > clip.Duration)
            time = clip.Duration;
        if (time < 0f)
            time = 0f;

        if (time < playback.LastTime && playback.KeyCursors.Length > 0)
            Array.Clear(playback.KeyCursors);
        playback.LastTime = time;
        playback.Time = time;
        return true;
    }

    private static void SampleLocals(SkinnedAnimationComponent playback, SkinData skin, ClipData clip)
    {
        for (var i = 0; i < skin.Joints.Length; i++)
        {
            var joint = skin.Joints[i];
            var translation = joint.RestTranslation;
            var rotation = joint.RestRotation;
            var scale = joint.RestScale;
            foreach (var channel in clip.Channels)
            {
                if (channel.Joint != i)
                    continue;
                Sample(channel, playback.Time, playback.KeyCursors, ref translation, ref rotation, ref scale);
            }

            playback.Locals[i] = LocalMatrix(translation, rotation, scale);
        }
    }

    private static void Sample(
        ClipChannel channel,
        float time,
        int[] cursors,
        ref Vector3 translation,
        ref Quaternion rotation,
        ref Vector3 scale)
    {
        var count = channel.Times.Length;
        if (count == 0)
            return;

        var cursorIndex = channel.Joint * 3 + ((int)channel.Property - 1);
        var cursor = cursorIndex >= 0 && cursorIndex < cursors.Length ? cursors[cursorIndex] : 0;
        if (cursor < 0 || cursor >= count)
            cursor = 0;
        while (cursor + 1 < count && channel.Times[cursor + 1] <= time)
            cursor++;
        if (cursorIndex >= 0 && cursorIndex < cursors.Length)
            cursors[cursorIndex] = cursor;

        var stride = channel.Property == ChannelProperty.Rotation ? 4 : 3;
        if (channel.Values.Length < count * stride)
            return;

        var useSingle = channel.Interpolation == ChannelInterpolation.Step
                        || cursor + 1 >= count
                        || time <= channel.Times[cursor];
        if (channel.Property == ChannelProperty.Rotation)
        {
            var from = ReadQuaternion(channel.Values, cursor * 4);
            if (useSingle)
            {
                rotation = from;
                return;
            }

            var next = cursor + 1;
            var span = channel.Times[next] - channel.Times[cursor];
            var t = span > 0f ? (time - channel.Times[cursor]) / span : 0f;
            rotation = Quaternion.Slerp(from, ReadQuaternion(channel.Values, next * 4), t);
            return;
        }

        var a = ReadVector3(channel.Values, cursor * 3);
        Vector3 value;
        if (useSingle)
            value = a;
        else
        {
            var next = cursor + 1;
            var span = channel.Times[next] - channel.Times[cursor];
            var t = span > 0f ? (time - channel.Times[cursor]) / span : 0f;
            value = Vector3.Lerp(a, ReadVector3(channel.Values, next * 3), t);
        }

        if (channel.Property == ChannelProperty.Scale)
            scale = value;
        else
            translation = value;
    }

    private static void BuildPalette(SkinData skin, ReadOnlySpan<Matrix4x4> locals, Matrix4x4[] destination)
    {
        var count = skin.Joints.Length;
        for (var i = 0; i < count; i++)
            destination[i] = skin.Joints[i].InverseBind * Global(skin, locals, i, 0);
    }

    private static Matrix4x4 Global(SkinData skin, ReadOnlySpan<Matrix4x4> locals, int index, int depth)
    {
        var local = locals[index];
        var parent = skin.Joints[index].Parent;
        if (parent < 0 || parent >= skin.Joints.Length || depth >= skin.Joints.Length)
            return local;
        return local * Global(skin, locals, parent, depth + 1);
    }

    private static Matrix4x4 LocalMatrix(Vector3 translation, Quaternion rotation, Vector3 scale) =>
        Matrix4x4.CreateScale(scale)
        * Matrix4x4.CreateFromQuaternion(rotation)
        * Matrix4x4.CreateTranslation(translation);

    private static Vector3 ReadVector3(float[] values, int offset) =>
        new(values[offset], values[offset + 1], values[offset + 2]);

    private static Quaternion ReadQuaternion(float[] values, int offset) =>
        new(values[offset], values[offset + 1], values[offset + 2], values[offset + 3]);
}
