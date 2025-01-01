using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

public struct LineVertex
{
    public int EntityId { get; init; }
    public Vector3 Position { get; init; }
    public Vector4 Color { get; init; }

    public static int GetSize() => Marshal.SizeOf(typeof(LineVertex));
}