using System.Numerics;
using Serilog;
using Serilog.Events;

namespace Engine.Scene.Skeletal;

/// <summary>Throttled skeletal render logs — enable Debug in console to see.</summary>
internal static class SkinnedRenderDiagnostics
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SkinnedRenderDiagnostics));
    private static readonly HashSet<string> LoggedOnce = [];
    private static int _renderFrame;

    public static bool DebugEnabled => Logger.IsEnabled(LogEventLevel.Debug);

    public static void Reset()
    {
        LoggedOnce.Clear();
        _renderFrame = 0;
    }

    public static void OnRenderFrame() => _renderFrame++;

    public static bool EveryNFrames(int n) => n > 0 && _renderFrame % n == 0;

    public static void Once(string key, Action write)
    {
        if (!LoggedOnce.Add(key))
            return;
        write();
    }

    public static void LogBonePalette(string source, Matrix4x4[] palette, int sampleBones = 3, params int[] extraBoneIndices)
    {
        if (!DebugEnabled)
            return;

        var maxT = 0f;
        var nonIdentity = 0;
        var maxDeviation = 0f;
        for (var i = 0; i < palette.Length; i++)
        {
            if (palette[i] != Matrix4x4.Identity)
                nonIdentity++;
            var t = new Vector3(palette[i].M41, palette[i].M42, palette[i].M43);
            maxT = MathF.Max(maxT, t.Length());
            maxDeviation = MathF.Max(maxDeviation, MatrixDeviationFromIdentity(palette[i]));
        }

        var indices = new SortedSet<int>();
        for (var i = 0; i < System.Math.Min(sampleBones, palette.Length); i++)
            indices.Add(i);
        foreach (var idx in extraBoneIndices)
        {
            if (idx >= 0 && idx < palette.Length)
                indices.Add(idx);
        }

        foreach (var i in indices)
        {
            var m = palette[i];
            Logger.Debug(
                "SkinnedDbg palette {Source} bone[{Index}] row4=({M41:F4},{M42:F4},{M43:F4}) col4=({M14:F4},{M24:F4},{M34:F4}) diag=({M11:F4},{M22:F4},{M33:F4}) dev={Dev:F4}",
                source, i, m.M41, m.M42, m.M43, m.M14, m.M24, m.M34, m.M11, m.M22, m.M33,
                MatrixDeviationFromIdentity(m));
        }

        Logger.Debug(
            "SkinnedDbg palette {Source} summary nonIdentity={NonIdentity}/{Total} maxTranslation={MaxT:F4} maxDevFromI={MaxDev:F4}",
            source, nonIdentity, palette.Length, maxT, maxDeviation);
    }

    public static float MatrixDeviationFromIdentity(Matrix4x4 m)
    {
        var d = 0f;
        d += MathF.Abs(m.M11 - 1f) + MathF.Abs(m.M22 - 1f) + MathF.Abs(m.M33 - 1f) + MathF.Abs(m.M44 - 1f);
        d += MathF.Abs(m.M12) + MathF.Abs(m.M13) + MathF.Abs(m.M14);
        d += MathF.Abs(m.M21) + MathF.Abs(m.M23) + MathF.Abs(m.M24);
        d += MathF.Abs(m.M31) + MathF.Abs(m.M32) + MathF.Abs(m.M34);
        d += MathF.Abs(m.M41) + MathF.Abs(m.M42) + MathF.Abs(m.M43);
        return d;
    }

    public static void LogMatrix(string label, Matrix4x4 m) =>
        Logger.Debug(
            "SkinnedDbg {Label} row4=({M41:F4},{M42:F4},{M43:F4}) diag=({M11:F4},{M22:F4},{M33:F4}) M14=({M14:F4})",
            label, m.M41, m.M42, m.M43, m.M11, m.M22, m.M33, m.M14);
}
