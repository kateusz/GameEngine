using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using NSubstitute;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class Graphics3DSkyboxTests
{
    [Fact]
    public void SetSkybox_SamePathSkipsCapture_FailedPathKeepsPrevious()
    {
        var skyShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Skybox).Returns(skyShader);

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var capture = Substitute.For<ISkyCapture>();
        capture.Begin(Arg.Any<uint>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(true);
        capture.GenerateEnvironmentMips().Returns(true);
        capture.IrradianceId.Returns(2u);
        capture.PrefilterId.Returns(3u);

        var attempt = 0;
        var renderer = Substitute.For<IRendererAPI>();
        renderer.TryCreateSkyCapture(Arg.Any<string>(), out Arg.Any<uint>(), out Arg.Any<ISkyCapture>())
            .Returns(call =>
            {
                attempt++;
                if (attempt == 1)
                {
                    call[1] = 11u;
                    call[2] = capture;
                    return true;
                }

                call[1] = 0u;
                call[2] = null!;
                return false;
            });

        var graphics = new Graphics3D(
            renderer,
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
        graphics.Init();

        var first = Path.Combine(Path.GetTempPath(), "ibl-a.hdr");
        var second = Path.Combine(Path.GetTempPath(), "ibl-b.hdr");
        graphics.SetSkybox(first);
        graphics.SetSkybox(first);
        renderer.Received(1).TryCreateSkyCapture(Arg.Any<string>(), out Arg.Any<uint>(), out Arg.Any<ISkyCapture>());

        graphics.SetSkybox(second);
        renderer.DidNotReceive().DeleteTexture(11u);
        graphics.DrawSkybox(Matrix4x4.Identity);
        skyShader.Received().Bind();

        graphics.SetSkybox(second);
        renderer.Received(2).TryCreateSkyCapture(Arg.Any<string>(), out Arg.Any<uint>(), out Arg.Any<ISkyCapture>());
    }

    private static Mesh InitializedMesh()
    {
        var vao = Substitute.For<IVertexArray>();
        var vbo = Substitute.For<IVertexBuffer>();
        var ibo = Substitute.For<IIndexBuffer>();
        ibo.Count.Returns(36);
        vao.IndexBuffer.Returns(ibo);

        var vaoFactory = Substitute.For<IVertexArrayFactory>();
        vaoFactory.Create().Returns(vao);
        var vboFactory = Substitute.For<IVertexBufferFactory>();
        vboFactory.Create(Arg.Any<List<Mesh.Vertex>>()).Returns(vbo);
        var iboFactory = Substitute.For<IIndexBufferFactory>();
        iboFactory.Create(Arg.Any<uint[]>(), Arg.Any<int>()).Returns(ibo);

        var mesh = new Mesh("cube");
        mesh.Vertices.Add(default);
        mesh.Indices.Add(0);
        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        return mesh;
    }
}
