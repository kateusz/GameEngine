using System.Numerics;
using Engine.Core;
using Engine.Platform;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using Engine.Scene;
using Engine.Scene.Cameras;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class Graphics3DTests : IDisposable
{
    public Graphics3DTests()
    {
        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(OperatingSystem.IsWindows() ? @"C:\game\assets" : "/game/assets");
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose() => PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());

    [Fact]
    public void TwoDrawCubes_ReusesSceneState_UploadsViewProjectionOnce()
    {
        var rendererApi = Substitute.For<IRendererAPI>();
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var skyboxShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>())
            .Returns(cubeShader, modelShader, skyboxShader);

        var cubeMesh = CreateInitializedMesh("cube", indexCount: 36);
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cubeMesh);

        var textureFactory = Substitute.For<ITextureFactory>();
        var vertexArrayFactory = Substitute.For<IVertexArrayFactory>();
        vertexArrayFactory.Create().Returns(Substitute.For<IVertexArray>());

        var graphics3D = new Graphics3D(rendererApi, shaderFactory, meshFactory, textureFactory, vertexArrayFactory);
        graphics3D.Init();

        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -10f, 10f);
        camera.SetViewportSize(800, 600);

        graphics3D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
        graphics3D.SetAmbientLight(Vector3.One, 0.1f);
        graphics3D.SetDirectionalLight(new Vector3(0, -1, 0), Vector3.Zero);
        graphics3D.DrawCube(Matrix4x4.Identity, Vector4.One);
        graphics3D.DrawCube(Matrix4x4.Identity, Vector4.One);
        graphics3D.EndScene();

        rendererApi.Received(1).SetDepthTest(true);
        cubeShader.Received(1).Bind();
        cubeShader.Received(1).Unbind();
        cubeShader.Received(1).SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.Received(2).SetMat4("u_Model", Arg.Any<Matrix4x4>());
        rendererApi.Received(2).DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
        graphics3D.GetStats().DrawCalls.ShouldBe(2u);
        graphics3D.GetStats().CulledDraws.ShouldBe(0u);
    }

    [Fact]
    public void DrawCube_InFront_IncrementsDrawCalls_OffToTheSide_IncrementsCulledOnly()
    {
        var (graphics3D, rendererApi) = CreateGraphics3D();
        BeginPerspective(graphics3D);

        graphics3D.DrawCube(Matrix4x4.CreateTranslation(0f, 0f, -5f), Vector4.One);
        graphics3D.DrawCube(Matrix4x4.CreateTranslation(100f, 0f, -5f), Vector4.One);
        graphics3D.EndScene();

        rendererApi.Received(1).DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
        graphics3D.GetStats().DrawCalls.ShouldBe(1u);
        graphics3D.GetStats().CulledDraws.ShouldBe(1u);
    }

    [Fact]
    public void NonFiniteViewProjection_DrawsInsteadOfCulling()
    {
        var (graphics3D, rendererApi) = CreateGraphics3D();
        var nan = Matrix4x4.Identity;
        nan.M11 = float.NaN;

        graphics3D.BeginScene(new SceneView(nan, Vector3.Zero));
        graphics3D.DrawCube(Matrix4x4.CreateTranslation(100f, 0f, -5f), Vector4.One);
        graphics3D.EndScene();

        rendererApi.Received(1).DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
        graphics3D.GetStats().DrawCalls.ShouldBe(1u);
        graphics3D.GetStats().CulledDraws.ShouldBe(0u);
    }

    [Fact]
    public void EmptyMesh_SkipsDrawWithoutCountingAsCulled()
    {
        var (graphics3D, rendererApi) = CreateGraphics3D(indexCount: 0);
        BeginPerspective(graphics3D);

        graphics3D.DrawMesh(Matrix4x4.CreateTranslation(0f, 0f, -5f), CreateInitializedMesh("empty", 0), Vector4.One);
        graphics3D.EndScene();

        rendererApi.DidNotReceive().DrawIndexed(Arg.Any<IVertexArray>(), Arg.Any<uint>());
        graphics3D.GetStats().DrawCalls.ShouldBe(0u);
        graphics3D.GetStats().CulledDraws.ShouldBe(0u);
    }

    private static (Graphics3D Graphics, IRendererAPI RendererApi) CreateGraphics3D(int indexCount = 36)
    {
        var rendererApi = Substitute.For<IRendererAPI>();
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var skyboxShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>())
            .Returns(cubeShader, modelShader, skyboxShader);

        var cubeMesh = CreateInitializedMesh("cube", indexCount);
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cubeMesh);

        var vertexArrayFactory = Substitute.For<IVertexArrayFactory>();
        vertexArrayFactory.Create().Returns(Substitute.For<IVertexArray>());

        var graphics3D = new Graphics3D(rendererApi, shaderFactory, meshFactory, Substitute.For<ITextureFactory>(),
            vertexArrayFactory);
        graphics3D.Init();
        return (graphics3D, rendererApi);
    }

    private static void BeginPerspective(Graphics3D graphics3D)
    {
        var camera = new SceneCamera();
        camera.SetPerspective(MathF.PI / 4f, 0.1f, 100f);
        camera.SetViewportSize(800, 600);
        graphics3D.BeginScene(CameraViews.From(camera, Matrix4x4.Identity));
    }

    private static Mesh CreateInitializedMesh(string name, int indexCount)
    {
        var vao = Substitute.For<IVertexArray>();
        var vbo = Substitute.For<IVertexBuffer>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(indexCount);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(vbo);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        var mesh = new Mesh(name);
        mesh.Vertices.Add(default);
        mesh.Indices.AddRange([0u, 1u, 2u]);
        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        return mesh;
    }
}
