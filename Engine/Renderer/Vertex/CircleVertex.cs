using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer.Vertex;

public struct CircleVertex
{
    public Vector3 WorldPosition { get; init; }
    public Vector3 LocalPosition { get; init; }
    public Vector4 Color { get; init; }
    public float Thickness { get; init; }
    public float Fade { get; init; }
    public int EntityId { get; init; }

    public static int GetSize() => Marshal.SizeOf<QuadVertex>();
}