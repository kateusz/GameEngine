using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Silk.NET.OpenGL;
using Buffer = System.Buffer;

namespace Engine.Platform.SilkNet.Buffers;

public class SilkNetUniformBuffer : IUniformBuffer
{
    private readonly uint _rendererId;

    public SilkNetUniformBuffer(uint size, uint binding)
    {
        unsafe
        {
            // Generate buffer
            _rendererId = SilkNetContext.GL.GenBuffer();

            // Allocate buffer storage
            SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _rendererId);
            SilkNetContext.GL.BufferData(BufferTargetARB.UniformBuffer, size, null, BufferUsageARB.DynamicDraw);

            // Bind buffer to a binding point
            SilkNetContext.GL.BindBufferBase(BufferTargetARB.UniformBuffer, binding, _rendererId);
        }
    }

    public void SetData(CameraData cameraData, int dataSize, int offset = 0)
    {
        SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _rendererId);

        var data = MatrixToByteArray(cameraData.ViewProjection);
       
        unsafe
        {
            fixed (byte* pData = data)
            {
                SilkNetContext.GL.BufferSubData(BufferTargetARB.UniformBuffer, (nint)offset, (nuint)dataSize, pData);
            }
        }
        
    }
    
    static byte[] MatrixToByteArray(Matrix4x4 matrix)
    {
        // A Matrix4x4 contains 16 float values. Each float is 4 bytes.
        byte[] result = new byte[16 * sizeof(float)];
        Buffer.BlockCopy(new[]
        {
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
            matrix.M21, matrix.M22, matrix.M23, matrix.M24,
            matrix.M31, matrix.M32, matrix.M33, matrix.M34,
            matrix.M41, matrix.M42, matrix.M43, matrix.M44
        }, 0, result, 0, result.Length);

        return result;
    }
}