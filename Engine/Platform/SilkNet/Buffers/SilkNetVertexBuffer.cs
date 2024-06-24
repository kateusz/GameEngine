using Engine.Renderer;
using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetVertexBuffer : IVertexBuffer
{
    private readonly uint _rendererId;

    public SilkNetVertexBuffer(uint size)
    {
        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);
        SilkNetContext.GL.BufferData(GLEnum.ArrayBuffer, size, IntPtr.Zero, GLEnum.DynamicDraw);
    }

    ~SilkNetVertexBuffer()
    {
        SilkNetContext.GL.DeleteBuffers(1, _rendererId);
    }

    public void SetLayout(BufferLayout layout)
    {
        Layout = layout;
    }

    public BufferLayout Layout { get; private set; }

    public void SetData(QuadVertex[] vertexes, uint dataSize)
    {
        if (vertexes.Length == 0)
            return;
        
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        var floats = new List<float>();
        
        foreach (var quadVertex in vertexes)
        {
            floats.AddRange(quadVertex.GetFloatArray());
        }
        
        var bufferData = floats.ToArray();
        
        unsafe
        {
            fixed (float* pNewData = bufferData)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(bufferData.Length * sizeof(float)),
                    pNewData, BufferUsageARB.StaticDraw);
            }
        }
    }

    public void Bind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);
    }

    public void Unbind()
    {
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, 0);
    }
}