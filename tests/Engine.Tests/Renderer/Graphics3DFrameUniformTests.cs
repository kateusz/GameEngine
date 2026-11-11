using System.Numerics;
using System.Runtime.InteropServices;
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
public class Graphics3DFrameUniformTests
{
    [Fact]
    public void BeginScene_UploadsViewAndLights_DrawCubeDoesNotRepeatThem()
    {
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var renderer = FakeUniformBuffer.Renderer(out var frame);
        var graphics = new Graphics3D(
            renderer,
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

        renderer.Received().CreateUniformBuffer(FrameUniforms.ByteSize);
        frame.SetDataCount.ShouldBe(1);
        frame.BindToCount.ShouldBe(1);
        frame.Binding.ShouldBe(0u);
        frame.Last.Length.ShouldBe(FrameUniforms.ByteSize);
        var uploaded = MemoryMarshal.Read<FrameUniforms>(frame.Last);
        uploaded.ViewProjection.ShouldBe(Matrix4x4.Transpose(vp));
        uploaded.ViewPosition.ShouldBe(new Vector4(viewPos, 0f));
        uploaded.AmbientColorStrength.ShouldBe(new Vector4(ambient, 0.5f));
        uploaded.LightDirection.ShouldBe(new Vector4(lightDir, 0f));
        uploaded.LightColor.ShouldBe(new Vector4(lightColor, 0f));
        renderer.DidNotReceive().BindTextureCube(Arg.Any<uint>(), 13);
        renderer.DidNotReceive().BindTextureCube(Arg.Any<uint>(), 14);
        renderer.DidNotReceive().BindTexture2D(Arg.Any<uint>(), 15);

        cubeShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        modelShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());

        frame.SetDataCount.ShouldBe(1);
        frame.BindToCount.ShouldBe(1);

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);

        frame.SetDataCount.ShouldBe(1);
        frame.BindToCount.ShouldBe(1);
        cubeShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.DidNotReceive().SetMat4("u_LightViewProjection", Arg.Any<Matrix4x4>());
        cubeShader.DidNotReceive().SetFloat3("u_AmbientColor", Arg.Any<Vector3>());
        cubeShader.Received().SetMat4("u_Model", Matrix4x4.Identity);
    }

    [Fact]
    public void BeginScene_PacksTransposedViewProjection_ForNonsymmetricCamera()
    {
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Cube).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Model).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var renderer = FakeUniformBuffer.Renderer(out var frame);
        var graphics = new Graphics3D(
            renderer,
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
        graphics.Init();

        var vp = Matrix4x4.CreateLookAt(new Vector3(2f, 3f, 5f), Vector3.Zero, Vector3.UnitY)
            * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4f, 1.2f, 0.1f, 50f);
        graphics.BeginScene(new SceneView(vp, new Vector3(2f, 3f, 5f)));

        var uploaded = MemoryMarshal.Read<FrameUniforms>(frame.Last);
        uploaded.ViewProjection.ShouldBe(Matrix4x4.Transpose(vp));
        uploaded.ViewProjection.ShouldNotBe(vp);
    }

    [Fact]
    public void BeginScene_WithDirectionalShadow_PacksShadowFieldsOnce()
    {
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var renderer = FakeUniformBuffer.Renderer(out var frame);
        var graphics = new Graphics3D(
            renderer,
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
        graphics.Init();

        var light = Matrix4x4.CreateLookAt(new Vector3(0f, 4f, 4f), Vector3.Zero, Vector3.UnitY)
            * Matrix4x4.CreateOrthographic(8f, 8f, 0.1f, 20f);
        graphics.SetDirectionalShadow(light, true);
        graphics.SetPointLights([
            new PointLightData(new Vector3(1f, 2f, 3f), new Vector3(0.1f, 0.2f, 0.3f), 4f, 5f)]);
        graphics.BeginScene(new SceneView(Matrix4x4.Identity, Vector3.Zero));

        frame.SetDataCount.ShouldBe(1);
        var uploaded = MemoryMarshal.Read<FrameUniforms>(frame.Last);
        uploaded.LightViewProjection.ShouldBe(Matrix4x4.Transpose(light));
        uploaded.ShadowsEnabled.ShouldBe(1);
        uploaded.PointLightCount.ShouldBe(1);
        uploaded.PointPositions[0].ShouldBe(new Vector4(1f, 2f, 3f, 0f));
        uploaded.PointColors[0].ShouldBe(new Vector4(0.1f, 0.2f, 0.3f, 0f));
        uploaded.PointIntensityRange[0].ShouldBe(new Vector4(4f, 5f, 0f, 0f));

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        frame.SetDataCount.ShouldBe(1);
        frame.BindToCount.ShouldBe(1);
    }

    [Fact]
    public void BeginScene_UploadsDirectionalShadowOnce()
    {
        var cubeShader = Substitute.For<IShader>();
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Cube).Returns(cubeShader);
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Depth).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.PointDepth).Returns(Substitute.For<IShader>());

        var meshFactory = Substitute.For<IMeshFactory>();
        var cube = InitializedMesh();
        meshFactory.CreateCube().Returns(cube);

        var graphics = new Graphics3D(
            FakeUniformBuffer.Renderer(out var frame),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            Substitute.For<IFrameBufferFactory>());
        graphics.Init();

        var light = Matrix4x4.CreateOrthographic(2f, 2f, 0.1f, 10f);
        graphics.SetDirectionalShadow(light, true);
        graphics.BeginScene(new SceneView(Matrix4x4.Identity, Vector3.Zero));

        frame.SetDataCount.ShouldBe(1);
        frame.Binding.ShouldBe(0u);
        cubeShader.DidNotReceive().SetMat4("u_LightViewProjection", Arg.Any<Matrix4x4>());
        modelShader.DidNotReceive().SetInt("u_ShadowsEnabled", Arg.Any<int>());

        cubeShader.ClearReceivedCalls();
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        frame.SetDataCount.ShouldBe(1);
        cubeShader.DidNotReceive().SetMat4("u_LightViewProjection", Arg.Any<Matrix4x4>());
    }

    [Fact]
    public void BeginShadowPass_DrawsWithDepthShader()
    {
        var cubeShader = Substitute.For<IShader>();
        var depthShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
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
            FakeUniformBuffer.Renderer(),
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

    [Fact]
    public void BeginPointShadowFace_DrawMesh_UsesPointDepthNotWindowDepth()
    {
        var depthShader = Substitute.For<IShader>();
        var pointDepthShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Cube).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Model).Returns(Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Depth).Returns(depthShader);
        shaderFactory.Create(ShaderId.PointDepth).Returns(pointDepthShader);

        var cube = InitializedMesh();
        var caster = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var cubemap = Substitute.For<IFrameBuffer>();
        var frameBuffers = Substitute.For<IFrameBufferFactory>();
        frameBuffers.Create(Arg.Any<FrameBufferSpecification>()).Returns(cubemap);

        var graphics = new Graphics3D(
            FakeUniformBuffer.Renderer(),
            shaderFactory,
            meshFactory,
            Substitute.For<ITextureFactory>(),
            frameBuffers);
        graphics.Init();

        var face = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 2f, 1f, 0.1f, 10f);
        graphics.BeginPointShadowFace(0, entityId: 7, face: 0, face, Vector3.Zero, range: 10f)
            .ShouldBeTrue();

        var model = Matrix4x4.CreateTranslation(1f, 2f, 3f);
        graphics.DrawMesh(model, caster, Vector4.One);

        pointDepthShader.Received().SetMat4("u_Model", model);
        depthShader.DidNotReceive().SetMat4("u_Model", Arg.Any<Matrix4x4>());

        graphics.EndPointShadowFace();
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
