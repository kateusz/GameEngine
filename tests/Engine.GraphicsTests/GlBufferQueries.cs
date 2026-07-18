using Engine.Platform.SilkNet;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

internal static class GlBufferQueries
{
    private static GL Gl => SilkNetContext.GL;

    public static int GetBufferSize(uint bufferId) =>
        (int)QueryBuffer(bufferId, GLEnum.ArrayBuffer, GLEnum.BufferSize);

    public static BufferUsageARB GetBufferUsage(uint bufferId) =>
        (BufferUsageARB)QueryBuffer(bufferId, GLEnum.ArrayBuffer, GLEnum.BufferUsage);

    public static int GetIndexBufferSize(uint bufferId) =>
        (int)QueryBuffer(bufferId, GLEnum.ElementArrayBuffer, GLEnum.BufferSize);

    public static BufferUsageARB GetIndexBufferUsage(uint bufferId) =>
        (BufferUsageARB)QueryBuffer(bufferId, GLEnum.ElementArrayBuffer, GLEnum.BufferUsage);

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

    private static long QueryBuffer(uint bufferId, GLEnum target, GLEnum parameter)
    {
        Gl.BindBuffer(target, bufferId);
        return Gl.GetBufferParameter(target, parameter);
    }
}
