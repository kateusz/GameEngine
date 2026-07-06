using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer.Primitives;

[StructLayout(LayoutKind.Sequential)]
public record struct LineVertex
{
    public Vector3 Position;
    public Vector4 Color;
    public int EntityId;

    public static int GetSize() => sizeof(float) * (3 + 4) + sizeof(int); // 32 bytes
}
