using Engine.Renderer;
using Engine.Renderer.Buffers;
using NSubstitute;

namespace Engine.Tests.Renderer;

internal sealed class FakeUniformBuffer : IUniformBuffer
{
    public byte[] Last = [];
    public uint Binding;
    public int SetDataCount { get; private set; }
    public int BindToCount { get; private set; }

    public void SetData(ReadOnlySpan<byte> data)
    {
        SetDataCount++;
        Last = data.ToArray();
    }

    public void BindTo(uint bindingPoint)
    {
        BindToCount++;
        Binding = bindingPoint;
    }
    public void Dispose() { }

    public static IRendererAPI Renderer(out FakeUniformBuffer ubo)
    {
        ubo = new FakeUniformBuffer();
        var renderer = Substitute.For<IRendererAPI>();
        renderer.CreateUniformBuffer(Arg.Any<int>()).Returns(ubo);
        return renderer;
    }

    public static IRendererAPI Renderer() => Renderer(out _);

    public static void Attach(IRendererAPI renderer) =>
        renderer.CreateUniformBuffer(Arg.Any<int>()).Returns(new FakeUniformBuffer());
}
