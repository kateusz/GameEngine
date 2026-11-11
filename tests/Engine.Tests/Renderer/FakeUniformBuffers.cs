using Engine.Renderer.Buffers;

namespace Engine.Tests.Renderer;

internal sealed class FakeUniformBuffer : IUniformBuffer
{
    public byte[] Last = [];
    public uint Binding;
    public int DisposeCount;

    public void SetData(ReadOnlySpan<byte> data) => Last = data.ToArray();
    public void BindTo(uint bindingPoint) => Binding = bindingPoint;
    public void Dispose() => DisposeCount++;
}

internal sealed class FakeUniformBufferFactory(FakeUniformBuffer buffer) : IUniformBufferFactory
{
    public FakeUniformBuffer Buffer { get; } = buffer;
    public int LastSize { get; private set; }

    public IUniformBuffer Create(int byteSize)
    {
        LastSize = byteSize;
        return Buffer;
    }
}

internal static class FakeUniformBuffers
{
    public static FakeUniformBufferFactory Create(out FakeUniformBuffer buffer)
    {
        buffer = new FakeUniformBuffer();
        return new FakeUniformBufferFactory(buffer);
    }

    public static IUniformBufferFactory Create() => Create(out _);
}
