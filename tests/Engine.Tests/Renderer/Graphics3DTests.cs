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
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<string>(), Arg.Any<string>())
            .Returns(cubeShader, modelShader);

        var cubeMesh = CreateInitializedMesh("cube", indexCount: 36);
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cubeMesh);

        var textureFactory = Substitute.For<ITextureFactory>();

        var graphics3D = new Graphics3D(rendererApi, shaderFactory, meshFactory, textureFactory);
        graphics3D.Init();

        var camera = new SceneCamera();
        camera.SetOrthographic(10f, -10f, 10f);
        camera.SetViewportSize(800, 600);

        graphics3D.BeginScene(camera, Matrix4x4.Identity);
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
