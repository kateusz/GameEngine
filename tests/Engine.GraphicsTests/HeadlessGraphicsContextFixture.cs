using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.GraphicsTests.ImageRegression;
using Silk.NET.Windowing;

namespace Engine.GraphicsTests;

public sealed class HeadlessGraphicsContextFixture : IDisposable
{
    private static readonly IRendererApiConfig ApiConfig = new RendererApiConfig(ApiType.SilkNet);

    public IGraphicsContext GraphicsContext { get; }
    public IWindow Window { get; }
    public IRendererAPI RendererApi { get; }
    public IFrameBufferFactory FrameBufferFactory { get; }
    public IGraphics2D Graphics2D { get; }
    public IGraphics3D Graphics3D { get; }
    public IVertexBufferFactory VertexBufferFactory { get; }
    public IIndexBufferFactory IndexBufferFactory { get; }
    public IVertexArrayFactory VertexArrayFactory { get; }

    private readonly TextureFactory _textureFactory;
    private readonly ShaderFactory _shaderFactory;
    private readonly MeshFactory _meshFactory;

    public HeadlessGraphicsContextFixture()
    {
        GraphicsTestAssets.EnsureInitialized();

        Window = HeadlessWindow.Create("Engine.GraphicsTests");
        GraphicsContext = new SilkNetGraphicsContext();
        GraphicsContext.Create(Window);

        RendererApi = new RendererApiFactory(ApiConfig).Create();
        RendererApi.Init();

        VertexArrayFactory = new VertexArrayFactory(ApiConfig);
        VertexBufferFactory = new VertexBufferFactory(ApiConfig);
        IndexBufferFactory = new IndexBufferFactory(ApiConfig);
        _textureFactory = new TextureFactory(ApiConfig);
        _shaderFactory = new ShaderFactory(ApiConfig);
        _meshFactory = new MeshFactory(_textureFactory, VertexArrayFactory, VertexBufferFactory, IndexBufferFactory);
        FrameBufferFactory = new FrameBufferFactory(ApiConfig);

        Graphics2D = new Graphics2D(
            RendererApi,
            VertexArrayFactory,
            VertexBufferFactory,
            IndexBufferFactory,
            _textureFactory,
            _shaderFactory);
        Graphics2D.Init();

        Graphics3D = new Graphics3D(RendererApi, _shaderFactory, _meshFactory);
        Graphics3D.Init();
    }

    public void Dispose()
    {
        Graphics2D.Dispose();
        Graphics3D.Dispose();
        _meshFactory.Dispose();
        _shaderFactory.Dispose();
        _textureFactory.Dispose();
        GraphicsContext.Dispose();
        Window.Dispose();
    }
}
