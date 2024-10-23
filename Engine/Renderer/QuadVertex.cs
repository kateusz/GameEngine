using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

public struct QuadVertex
{
    public Vector3 Position { get; init; }
    public Vector4 Color { get; init; }
    public Vector2 TexCoord { get; init; }
    public float TexIndex { get; init; }
    public float TilingFactor { get; init; }
    public int EntityId { get; init; }

    public static int GetSize() => Marshal.SizeOf(typeof(QuadVertex));
}