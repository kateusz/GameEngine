using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

public sealed class HeadlessGraphicsContextFixture : IDisposable
{
    private static readonly IRendererApiConfig ApiConfig = new RendererApiConfig(ApiType.SilkNet);

    public IGraphicsContext GraphicsContext { get; }
    public IWindow Window { get; }
    public IVertexBufferFactory VertexBufferFactory { get; } = new VertexBufferFactory(ApiConfig);
    public IIndexBufferFactory IndexBufferFactory { get; } = new IndexBufferFactory(ApiConfig);
    public IVertexArrayFactory VertexArrayFactory { get; } = new VertexArrayFactory(ApiConfig);

    public HeadlessGraphicsContextFixture()
    {
        Window = HeadlessWindow.Create("Engine.GraphicsTests");

        GraphicsContext = new SilkNetGraphicsContext();
        GraphicsContext.Create(Window);
    }

    public void Dispose()
    {
        GraphicsContext.Dispose();
        Window.Dispose();
    }
}
