using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Shaders;
using Engine.Renderer.Textures;
using NSubstitute;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class Graphics3DFrameUniformTests
{
    [Fact]
    public void BeginScene_UploadsViewAndLights_DrawCubeDoesNotRepeatThem()
    {
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var graphics = new Graphics3D(
            Substitute.For<IRendererAPI>(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>());
        graphics.Init();

        var vp = Matrix4x4.CreateOrthographic(1, 1, 0.1f, 10f);
        var ambient = new Vector3(0.2f, 0.3f, 0.4f);
        var lightDir = new Vector3(0, -1, 0);
        var lightColor = Vector3.One;
        var viewPos = new Vector3(9, 8, 7);

        graphics.SetAmbientLight(ambient, 0.5f);
        graphics.SetDirectionalLight(lightDir, lightColor);
        graphics.BeginScene(new SceneView(vp, viewPos));

        cubeShader.Received().SetMat4("u_ViewProjection", vp);
        cubeShader.Received().SetFloat3("u_AmbientColor", ambient);
        cubeShader.Received().SetFloat("u_AmbientStrength", 0.5f);
        cubeShader.Received().SetFloat3("u_LightDirection", lightDir);
        cubeShader.Received().SetFloat3("u_LightColor", lightColor);
        cubeShader.DidNotReceive().SetFloat3("u_ViewPosition", Arg.Any<Vector3>());

        modelShader.Received().SetMat4("u_ViewProjection", vp);
        modelShader.Received().SetFloat3("u_ViewPosition", viewPos);
        modelShader.Received().SetFloat3("u_AmbientColor", ambient);

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        cubeShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.DidNotReceive().SetFloat3("u_AmbientColor", Arg.Any<Vector3>());
        cubeShader.Received().SetMat4("u_Model", Matrix4x4.Identity);
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
