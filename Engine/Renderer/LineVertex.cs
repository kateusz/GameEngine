using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

public record struct LineVertex(Vector3 Position, Vector4 Color, int EntityId)
{
    public static int GetSize() => Marshal.SizeOf<LineVertex>();
}