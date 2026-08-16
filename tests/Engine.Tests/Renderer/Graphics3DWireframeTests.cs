using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Renderer.Textures.EnvironmentMap;
using Engine.Scene.Cameras;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class Graphics3DWireframeTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly IRendererAPI _rendererApi;
    private readonly IShaderFactory _shaderFactory;
    private readonly IMeshFactory _meshFactory;
    private readonly ITextureFactory _textureFactory;
    private readonly IEnvironmentMapFactory _environmentMapFactory;
    private readonly IShader _cubeShader;
    private readonly IShader _texturedShader;
    private readonly IShader _skyboxShader;
    private readonly IShader _wireframeShader;
    private readonly Mesh _cubeMesh;
    private readonly Graphics3D _graphics;

    public Graphics3DWireframeTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GameEngine-Graphics3DWireframeTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempRoot, "assets"));

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(Path.Combine(_tempRoot, "assets"));
        PathBuilder.UseProjectContext(context);

        _rendererApi = Substitute.For<IRendererAPI>();
        _shaderFactory = Substitute.For<IShaderFactory>();
        _meshFactory = Substitute.For<IMeshFactory>();
        _textureFactory = Substitute.For<ITextureFactory>();
        _cubeShader = Substitute.For<IShader>();
        _texturedShader = Substitute.For<IShader>();
        _wireframeShader = Substitute.For<IShader>();
        _skyboxShader = Substitute.For<IShader>();
        _cubeMesh = CreateInitializedMesh("cube");

        IShader ResolveShader(NSubstitute.Core.CallInfo ci)
        {
            var vert = (string)ci[0];
            if (vert.Contains("wireframeShader", StringComparison.OrdinalIgnoreCase))
                return _wireframeShader;
            if (vert.Contains("skybox", StringComparison.OrdinalIgnoreCase))
                return _skyboxShader;
            if (vert.Contains("lightingShader", StringComparison.OrdinalIgnoreCase))
                return _texturedShader;
            return _cubeShader;
        }

        _shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>())
            .Returns(ci => ResolveShader(ci));
        _shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<ShaderDefine>>())
            .Returns(ci => ResolveShader(ci));

        _meshFactory.CreateCube().Returns(_cubeMesh);
        _textureFactory.GetWhiteTexture().Returns(new Texture2D());
        _textureFactory.GetBlackTexture().Returns(new Texture2D());
        _textureFactory.GetFlatNormalTexture().Returns(new Texture2D());
        _environmentMapFactory = Substitute.For<IEnvironmentMapFactory>();
        _environmentMapFactory.GetBlackCubemap().Returns(Substitute.For<TextureCube>());
        var vertexArrayFactory = Substitute.For<IVertexArrayFactory>();
        vertexArrayFactory.Create().Returns(Substitute.For<IVertexArray>());

        _graphics = new Graphics3D(_rendererApi, _shaderFactory, _meshFactory, _textureFactory, _environmentMapFactory, Substitute.For<IFrameBufferFactory>(), vertexArrayFactory);
        _graphics.Init();
    }

    public void Dispose()
    {
        _graphics.Dispose();
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ponytail: temp cleanup best-effort
        }
    }

    [Fact]
    public void SetWireframe_False_AlwaysRestoresFill()
    {
        _rendererApi.ClearReceivedCalls();
        _graphics.SetWireframe(false);
        _rendererApi.Received(1).SetPolygonMode(PolygonMode.Fill);

        _graphics.SetWireframe(true);
        _rendererApi.ClearReceivedCalls();
        _graphics.SetWireframe(false);
        _rendererApi.Received(1).SetPolygonMode(PolygonMode.Fill);
    }

    [Fact]
    public void DrawCube_WhenWireframe_SetsLineThenDrawThenFill()
    {
        BeginWithCamera();
        _graphics.SetWireframe(true);
        _rendererApi.ClearReceivedCalls();

        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One, entityId: 7);

        Received.InOrder(() =>
        {
            _rendererApi.SetPolygonMode(PolygonMode.Line);
            _rendererApi.DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
            _rendererApi.SetPolygonMode(PolygonMode.Fill);
        });
    }

    [Fact]
    public void DrawMesh_WhenWireframe_SetsLineThenDrawThenFill()
    {
        BeginWithCamera();
        _graphics.SetWireframe(true);
        var mesh = CreateInitializedMesh("mesh");
        _rendererApi.ClearReceivedCalls();

        _graphics.DrawMesh(Matrix4x4.Identity, mesh, new MeshMaterial(), Vector4.One, 0f, 0.5f, entityId: 3);

        Received.InOrder(() =>
        {
            _rendererApi.SetPolygonMode(PolygonMode.Line);
            _rendererApi.DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
            _rendererApi.SetPolygonMode(PolygonMode.Fill);
        });
    }

    [Fact]
    public void DrawCube_WhenNormal_NeverSetsPolygonModeLine()
    {
        BeginWithCamera();
        _rendererApi.ClearReceivedCalls();

        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        _rendererApi.DidNotReceive().SetPolygonMode(PolygonMode.Line);
        _rendererApi.Received(1).DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
    }

    [Fact]
    public void SetWireframe_True_LazyLoadsShaderOnce()
    {
        _shaderFactory.ClearReceivedCalls();

        _graphics.SetWireframe(true);
        _graphics.SetWireframe(true);
        BeginWithCamera();
        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        var hostVert = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "wireframeShader.vert"));
        var hostFrag = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "assets", "shaders", "OpenGL", "wireframeShader.frag"));
        _shaderFactory.Received(1).Create(hostVert, hostFrag);
    }

    [Fact]
    public void SetWireframe_True_WhenFactoryThrows_ClearsFlagAndBehavesAsNormal()
    {
        _shaderFactory.Create(
                Arg.Is<string>(p => p.Contains("wireframeShader", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("missing shader"));

        _graphics.SetWireframe(true);
        BeginWithCamera();
        _rendererApi.ClearReceivedCalls();

        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        _rendererApi.DidNotReceive().SetPolygonMode(PolygonMode.Line);
        _rendererApi.Received(1).DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
        _cubeShader.Received().Bind();
    }

    [Fact]
    public void SetWireframe_True_WhenFactoryThrows_DoesNotRetryAfterRestore()
    {
        _shaderFactory.Create(
                Arg.Is<string>(p => p.Contains("wireframeShader", StringComparison.OrdinalIgnoreCase)),
                Arg.Any<string>())
            .Returns(_ => throw new InvalidOperationException("missing shader"));

        _shaderFactory.ClearReceivedCalls();
        _graphics.SetWireframe(true);
        _graphics.SetWireframe(false);
        _graphics.SetWireframe(true);
        BeginWithCamera();
        _graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        _shaderFactory.Received(1).Create(
            Arg.Is<string>(p => p.Contains("wireframeShader", StringComparison.OrdinalIgnoreCase)),
            Arg.Any<string>());
    }

    [Fact]
    public void DrawCube_WhenWireframe_SetsCachedViewProjectionAndModel()
    {
        var viewProjection = Matrix4x4.CreateOrthographic(2f, 2f, 0.1f, 100f);
        var model = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        var camera = Substitute.For<IViewCamera>();
        camera.GetViewProjectionMatrix().Returns(viewProjection);
        camera.GetPosition().Returns(Vector3.Zero);

        _graphics.BeginScene(camera);
        _graphics.SetWireframe(true);
        _wireframeShader.ClearReceivedCalls();

        _graphics.DrawCube(model, new Vector4(0.2f, 0.3f, 0.4f, 1f), entityId: 9);

        _wireframeShader.Received().SetMat4("u_ViewProjection", viewProjection);
        _wireframeShader.Received().SetMat4("u_Model", model);
        _wireframeShader.Received().SetFloat4("u_Color", new Vector4(0.85f, 0.85f, 0.85f, 1f));
        _wireframeShader.Received().SetInt("u_EntityID", 9);
    }

    [Fact]
    public void Dispose_DisposesLazyLoadedWireframeShader()
    {
        _graphics.SetWireframe(true);
        _wireframeShader.ClearReceivedCalls();

        _graphics.Dispose();

        _wireframeShader.Received(1).Dispose();
    }

    private void BeginWithCamera()
    {
        var camera = Substitute.For<IViewCamera>();
        camera.GetViewProjectionMatrix().Returns(Matrix4x4.Identity);
        camera.GetPosition().Returns(Vector3.Zero);
        _graphics.BeginScene(camera);
    }

    private static Mesh CreateInitializedMesh(string name)
    {
        var mesh = new Mesh(name)
        {
            Vertices = [new Mesh.Vertex(Vector3.Zero, Vector3.UnitY, Vector2.Zero, Vector3.UnitX, Vector3.UnitZ)],
            Indices = [0, 0, 0]
        };

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        var vbFactory = Substitute.For<IVertexBufferFactory>();
        var ibFactory = Substitute.For<IIndexBufferFactory>();
        var vao = Substitute.For<IVertexArray>();
        var vb = Substitute.For<IVertexBuffer>();
        var ib = Substitute.For<IIndexBuffer>();

        vaoFactory.Create().Returns(vao);
        vbFactory.Create(Arg.Any<uint>()).Returns(vb);
        ibFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ib);

        mesh.Initialize(vaoFactory, vbFactory, ibFactory);
        return mesh;
    }
}
