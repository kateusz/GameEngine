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
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var graphics = new Graphics3D(
            Substitute.For<IRendererAPI>(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
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
        cubeShader.Received().SetFloat3("u_ViewPosition", viewPos);

        modelShader.Received().SetMat4("u_ViewProjection", vp);
        modelShader.Received().SetFloat3("u_ViewPosition", viewPos);
        modelShader.Received().SetFloat3("u_AmbientColor", ambient);

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        cubeShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.DidNotReceive().SetMat4("u_LightViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.DidNotReceive().SetFloat3("u_AmbientColor", Arg.Any<Vector3>());
        cubeShader.Received().SetMat4("u_Model", Matrix4x4.Identity);
    }

    [Fact]
    public void BeginScene_UploadsDirectionalShadowOnce()
    {
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var meshFactory = Substitute.For<IMeshFactory>();
        var cube = InitializedMesh();
        meshFactory.CreateCube().Returns(cube);

        var graphics = new Graphics3D(
            Substitute.For<IRendererAPI>(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
        graphics.Init();

        var light = Matrix4x4.CreateOrthographic(2f, 2f, 0.1f, 10f);
        graphics.SetDirectionalShadow(light, true);
        graphics.BeginScene(new SceneView(Matrix4x4.Identity, Vector3.Zero));

        cubeShader.Received().SetMat4("u_LightViewProjection", light);
        cubeShader.Received().SetInt("u_ShadowsEnabled", 1);
        modelShader.Received().SetMat4("u_LightViewProjection", light);
        modelShader.Received().SetInt("u_ShadowsEnabled", 1);

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        cubeShader.DidNotReceive().SetMat4("u_LightViewProjection", Arg.Any<Matrix4x4>());
    }

    [Fact]
    public void BeginShadowPass_DrawsWithDepthShader()
    {
        var cubeShader = Substitute.For<IShader>();
        var depthShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Depth).Returns(depthShader);
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var meshFactory = Substitute.For<IMeshFactory>();
        var cube = InitializedMesh();
        meshFactory.CreateCube().Returns(cube);

        var shadowMap = Substitute.For<IFrameBuffer>();
        var frameBuffers = Substitute.For<IFrameBufferFactory>();
        frameBuffers.Create(Arg.Any<FrameBufferSpecification>()).Returns(shadowMap);

        var graphics = new Graphics3D(
            Substitute.For<IRendererAPI>(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            frameBuffers);
        graphics.Init();

        var light = Matrix4x4.CreateOrthographic(2f, 2f, 0.1f, 10f);
        graphics.BeginShadowPass(light);

        shadowMap.Received().Bind();
        depthShader.Received().SetMat4("u_ViewProjection", light);

        var model = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        graphics.DrawCube(model, Vector4.One);

        depthShader.Received().SetMat4("u_Model", model);
        cubeShader.DidNotReceive().SetMat4(Arg.Any<string>(), Arg.Any<Matrix4x4>());

        graphics.EndShadowPass();
        shadowMap.Received().Unbind();
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
