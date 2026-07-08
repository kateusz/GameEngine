using Engine.Platform.SilkNet;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

internal static class GlBufferQueries
{
    private static GL Gl => SilkNetContext.GL;

    public static int GetBufferSize(uint bufferId)
    {
        Gl.BindBuffer(GLEnum.ArrayBuffer, bufferId);
        return (int)Gl.GetBufferParameter(GLEnum.ArrayBuffer, GLEnum.BufferSize);
    }

    public static BufferUsageARB GetBufferUsage(uint bufferId)
    {
        Gl.BindBuffer(GLEnum.ArrayBuffer, bufferId);
        return (BufferUsageARB)Gl.GetBufferParameter(GLEnum.ArrayBuffer, GLEnum.BufferUsage);
    }

    public static int GetIndexBufferSize(uint bufferId)
    {
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, bufferId);
        return (int)Gl.GetBufferParameter(GLEnum.ElementArrayBuffer, GLEnum.BufferSize);
    }

    public static BufferUsageARB GetIndexBufferUsage(uint bufferId)
    {
        Gl.BindBuffer(GLEnum.ElementArrayBuffer, bufferId);
        return (BufferUsageARB)Gl.GetBufferParameter(GLEnum.ElementArrayBuffer, GLEnum.BufferUsage);
    }

    public static bool IsBufferAlive(uint id) => Gl.IsBuffer(id);

    public static int GetAttribEnabled(uint index) =>
        (int)Gl.GetVertexAttrib(index, GLEnum.VertexAttribArrayEnabled);

    public static int GetAttribSize(uint index) =>
        (int)Gl.GetVertexAttrib(index, GLEnum.VertexAttribArraySize);

    public static int GetAttribStride(uint index) =>
        (int)Gl.GetVertexAttrib(index, GLEnum.VertexAttribArrayStride);

    public static nint GetAttribOffset(uint index)
    {
        unsafe
        {
            void* pointer;
            Gl.GetVertexAttribPointer(index, GLEnum.VertexAttribArrayPointer, &pointer);
            return (nint)pointer;
        }
    }

    public static uint GetElementArrayBufferBinding() =>
        (uint)Gl.GetInteger(GLEnum.ElementArrayBufferBinding);
}
