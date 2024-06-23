using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

public struct QuadVertex
{
    public Vector3 Position { get; init; }
    public Vector4 Color { get; init; }
    public Vector2 TexCoord { get; init; }

    public static uint GetSize()
    {
        return (uint)Marshal.SizeOf(typeof(QuadVertex));
    }
    
    public float[] GetFloatArray()
    {
        var floatArray = new float[9];
        
        floatArray[0] = Position.X;
        floatArray[1] = Position.Y;
        floatArray[2] = Position.Z;

        floatArray[3] = Color.X;
        floatArray[4] = Color.Y;
        floatArray[5] = Color.Z;
        floatArray[6] = Color.W;

        floatArray[7] = TexCoord.X;
        floatArray[8] = TexCoord.Y;

        return floatArray;
    }
}