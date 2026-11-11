using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

[InlineArray(8)]
public struct FrameVec4x8
{
    private Vector4 _e0;
}

[StructLayout(LayoutKind.Explicit, Size = ByteSize)]
public struct FrameUniforms
{
    [FieldOffset(0)] public Matrix4x4 ViewProjection;
    [FieldOffset(64)] public Matrix4x4 LightViewProjection;
    [FieldOffset(128)] public Matrix4x4 FarLightViewProjection;
    [FieldOffset(192)] public Vector4 AmbientColorStrength;
    [FieldOffset(208)] public Vector4 LightDirection;
    [FieldOffset(224)] public Vector4 LightColor;
    [FieldOffset(240)] public Vector4 ViewPosition;
    [FieldOffset(256)] public FrameVec4x8 PointPositions;
    [FieldOffset(384)] public FrameVec4x8 PointColors;
    [FieldOffset(512)] public FrameVec4x8 PointIntensityRange;
    [FieldOffset(640)] public int PointLightCount;
    [FieldOffset(644)] public int ShadowsEnabled;
    [FieldOffset(648)] public int CascadeCount;
    [FieldOffset(652)] public int ShadowPcf;
    [FieldOffset(656)] public float CascadeSplit;
    [FieldOffset(660)] public float ShadowMapSize;
    [FieldOffset(664)] public int PointPcf;
    [FieldOffset(668)] public float PointMapSize;
    [FieldOffset(672)] public int PointShadowMask;
    [FieldOffset(676)] public int Ibl;
    [FieldOffset(680)] public int Pad0;
    [FieldOffset(684)] public int Pad1;

    public const int ByteSize = 688;
}
