using Engine.Renderer.Buffers;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenTK;

public class OpenTKIndexBuffer : IIndexBuffer
{
    private readonly uint[] _indices;
    private readonly int _indexBuffer;

    public OpenTKIndexBuffer(uint[] indices, int count)
    {
        _indices = indices;
        Count = count;
        
        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
    }
    
    public int Count { get; }

    public void Bind()
    {
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);
    }

    public void Unbind()
    {
        // not needed in openTK
    }
}