using Engine.Platform.SilkNet;
using Engine.Renderer.Buffers;
using SceneComponents.Rendering;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL.Buffers;

internal sealed class OpenGLUniformBuffer : IUniformBuffer
{
    private readonly int _byteCapacity = SkinnedAnimationComponent.MaxJoints * 64;
    private uint _id;
    private bool _created;
    private bool _disposed;

    public void Update(ReadOnlySpan<float> std140)
    {
        if (std140.IsEmpty)
            return;

        Ensure();
        var bytes = System.Math.Min(std140.Length * sizeof(float), _byteCapacity);
        SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _id);
        unsafe
        {
            fixed (float* data = std140)
                SilkNetContext.GL.BufferSubData(BufferTargetARB.UniformBuffer, 0, (nuint)bytes, data);
        }
    }

    public void BindBase(int bindingPoint)
    {
        Ensure();
        SilkNetContext.GL.BindBufferBase(BufferTargetARB.UniformBuffer, (uint)bindingPoint, _id);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_created)
            SilkNetContext.GL.DeleteBuffer(_id);
        _disposed = true;
    }

    private void Ensure()
    {
        if (_created)
            return;

        _id = SilkNetContext.GL.GenBuffer();
        SilkNetContext.GL.BindBuffer(BufferTargetARB.UniformBuffer, _id);
        unsafe
        {
            SilkNetContext.GL.BufferData(
                BufferTargetARB.UniformBuffer, (nuint)_byteCapacity, null, BufferUsageARB.DynamicDraw);
        }

        _created = true;
    }
}
