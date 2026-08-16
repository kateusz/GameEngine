using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer.Pipeline.Primitives;

[StructLayout(LayoutKind.Sequential)]
public record struct QuadVertex
{
    public Vector3 Position;
    public Vector4 Color;
    public Vector2 TexCoord;
    public float TexIndex;
    public float TilingFactor;
    public int EntityId;

    public static int GetSize() => sizeof(float) * (3 + 4 + 2 + 1 + 1) + sizeof(int); // 48 bytes (3 vec4s)
}
