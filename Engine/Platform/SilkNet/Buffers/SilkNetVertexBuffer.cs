using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Vertex;
using Silk.NET.OpenGL;
using Buffer = System.Buffer;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetVertexBuffer : IVertexBuffer
{
    private readonly uint _rendererId;

    public SilkNetVertexBuffer(uint size)
    {
        _rendererId = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.ArrayBuffer, _rendererId);
        SilkNetContext.GL.BufferData(GLEnum.ArrayBuffer, size, in IntPtr.Zero, GLEnum.DynamicDraw);
    }

    ~SilkNetVertexBuffer()
    {
        SilkNetContext.GL.DeleteBuffers(1, _rendererId);
    }

    public void SetLayout(BufferLayout layout)
    {
        Layout = layout;
    }

    public BufferLayout? Layout { get; private set; }

    public void SetData(QuadVertex[] vertices, int dataSize)
    {
        if (vertices.Length == 0)
            return;
        
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        // Create a flat array of bytes from the vertices
        var data = new byte[dataSize];
        
        // Copy each vertex's data to the byte array
        for (int i = 0; i < vertices.Length; i++)
        {
            var vertex = vertices[i];

            // Convert each field to bytes and copy to the byte array
            Buffer.BlockCopy(new[] { vertex.Position.X, vertex.Position.Y, vertex.Position.Z }, 
                0, 
                data,
                i * QuadVertex.GetSize() + 0 * sizeof(float), 
                sizeof(float) * 3); // Position
            
            Buffer.BlockCopy(new[] { vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Color.W }, 
                0, 
                data,
                i * QuadVertex.GetSize() + 3 * sizeof(float), 
                sizeof(float) * 4); // Color
            
            Buffer.BlockCopy(new[] { vertex.TexCoord.X, vertex.TexCoord.Y }, 
                0, 
                data,
                i * QuadVertex.GetSize() + 7 * sizeof(float), 
                sizeof(float) * 2); // TexCoord
            
            Buffer.BlockCopy(new[] { vertex.TexIndex }, 
                0, 
                data, 
                i * QuadVertex.GetSize() + 9 * sizeof(float),
                sizeof(float)); // TexIndex
            
            Buffer.BlockCopy(new[] { vertex.TilingFactor }, 
                0, 
                data, 
                i * QuadVertex.GetSize() + 10 * sizeof(float),
                sizeof(float)); // TilingFactor
            
            Buffer.BlockCopy(new[] { vertex.EntityId }, 
                0, 
                data, 
                i * QuadVertex.GetSize() + 11 * sizeof(float),
                sizeof(int)); // EntityId
        }
        
        unsafe
        {
            fixed (byte* pData = data)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)data.Length, pData, BufferUsageARB.StaticDraw);
            }
        }
    }

    public void SetData(LineVertex[] lineVertices, int dataSize)
    {
        if (lineVertices.Length == 0)
            return;
        
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        // Create a flat array of bytes from the vertices
        var data = new byte[dataSize];
        
        // Copy each vertex's data to the byte array
        for (int i = 0; i < lineVertices.Length; i++)
        {
            var vertex = lineVertices[i];

            // Convert each field to bytes and copy to the byte array
            Buffer.BlockCopy(new[] { vertex.Position.X, vertex.Position.Y, vertex.Position.Z }, 
                0, 
                data,
                i * LineVertex.GetSize() + 0 * sizeof(float), 
                sizeof(float) * 3); // Position
            
            Buffer.BlockCopy(new[] { vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Color.W }, 
                0, 
                data,
                i * LineVertex.GetSize() + 3 * sizeof(float), 
                sizeof(float) * 4); // Color
            
            Buffer.BlockCopy(new[] { vertex.EntityId }, 
                0, 
                data, 
                i * LineVertex.GetSize() + 7 * sizeof(float),
                sizeof(int)); // EntityId
        }
        
        unsafe
        {
            fixed (byte* pData = data)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)data.Length, pData, BufferUsageARB.StaticDraw);
                var error = SilkNetContext.GL.GetError();
            }
        }
    }

    public void SetData(CircleVertex[] circleVertices, int dataSize)
    {
        if (circleVertices.Length == 0)
            return;
        
        SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, _rendererId);

        // Create a flat array of bytes from the vertices
        var data = new byte[dataSize];
        
        // Copy each vertex's data to the byte array
        for (int i = 0; i < circleVertices.Length; i++)
        {
            var vertex = circleVertices[i];

            // Convert each field to bytes and copy to the byte array
            Buffer.BlockCopy(new[] { vertex.WorldPosition.X, vertex.WorldPosition.Y, vertex.WorldPosition.Z }, 
                0, 
                data,
                i * CircleVertex.GetSize() + 0 * sizeof(float), 
                sizeof(float) * 3); // WorldPosition
            
            Buffer.BlockCopy(new[] { vertex.LocalPosition.X, vertex.LocalPosition.Y, vertex.LocalPosition.Z }, 
                0, 
                data,
                i * CircleVertex.GetSize() + 3 * sizeof(float), 
                sizeof(float) * 3); // LocalPosition
            
            Buffer.BlockCopy(new[] { vertex.Color.X, vertex.Color.Y, vertex.Color.Z, vertex.Color.W }, 
                0, 
                data,
                i * CircleVertex.GetSize() + 6 * sizeof(float), 
                sizeof(float) * 4); // ColorB
            
            Buffer.BlockCopy(new[] { vertex.Thickness }, 
                0, 
                data, 
                i * CircleVertex.GetSize() + 10 * sizeof(float),
                sizeof(float)); // Thickness
            
            Buffer.BlockCopy(new[] { vertex.Fade }, 
                0, 
                data, 
                i * CircleVertex.GetSize() + 11 * sizeof(float),
                sizeof(int)); // Fade
            
            Buffer.BlockCopy(new[] { vertex.EntityId }, 
                0, 
                data, 
                i * CircleVertex.GetSize() + 12 * sizeof(float),
                sizeof(int)); // EntityId
        }
        
        unsafe
        {
            fixed (byte* pData = data)
            {
                SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)data.Length, pData, BufferUsageARB.StaticDraw);
                var error = SilkNetContext.GL.GetError();
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