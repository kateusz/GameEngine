using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

/// <summary>
/// One instance of a shared mesh. Matrices are stored transposed so each attribute column matches
/// <c>glUniformMatrix</c> with <c>transpose = true</c>. The shader still multiplies <c>vec4 * mat4</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MeshInstanceData
{
    public Matrix4x4 Model;
    public Matrix4x4 Normal;
    public int EntityId;
    public int Pad0;
    public int Pad1;
    public int Pad2;

    public const int NormalByteOffset = 64;
    public const int EntityIdByteOffset = 128;
}
