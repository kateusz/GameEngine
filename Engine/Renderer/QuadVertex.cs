using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

public record struct QuadVertex(
    Vector3 Position, 
    Vector4 Color, 
    Vector2 TexCoord, 
    float TexIndex, 
    float TilingFactor, 
    int EntityId)
{
    public static int GetSize() => Marshal.SizeOf<QuadVertex>();
}
