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
public class Graphics3DStatsTests
{
    [Fact]
    public void DrawCube_And_DrawMesh_RecordPassAndGeometryStats()
    {
        var graphics = CreateGraphics3D(out var cube);
        graphics.Init();
        graphics.ResetStats();
        graphics.BeginScene(new SceneView(Matrix4x4.Identity, Vector3.Zero));

        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        graphics.DrawMesh(Matrix4x4.Identity, cube, Vector4.One);
        graphics.DrawMesh(Matrix4x4.CreateTranslation(1, 0, 0), cube, Vector4.One);

        var stats = graphics.GetStats();
        stats.DrawCalls.ShouldBe(3u);
        stats.ColorDrawCalls.ShouldBe(3u);
        stats.CubeDraws.ShouldBe(1u);
        stats.MeshDraws.ShouldBe(2u);
        stats.InstancedDraws.ShouldBe(0u);
        stats.Instances.ShouldBe(3u);
        stats.Vertices.ShouldBe(cube.VertexCount * 3L);
        stats.Triangles.ShouldBe(cube.GetIndexCount());
    }

    [Fact]
    public void ColorPass_KeepsProgramAndCullingUntilTheyChange()
    {
        var renderer = FakeUniformBuffer.Renderer();
        var modelShader = Substitute.For<IShader>();
        var emissiveShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Emissive).Returns(emissiveShader);
        using var graphics = CreateGraphics3D(out var mesh, shaderFactory: shaderFactory, renderer: renderer);
        using var doubleSided = InitializedMesh();
        using var emissiveTexture = Substitute.For<Texture2D>();
        doubleSided.DoubleSided = true;
        doubleSided.EmissiveTexture = emissiveTexture;
        doubleSided.EmissiveFactor = Vector3.One;
        graphics.Init();
        modelShader.ClearReceivedCalls();
        emissiveShader.ClearReceivedCalls();
        renderer.ClearReceivedCalls();

        graphics.BeginScene(new SceneView(Matrix4x4.Identity));
        graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);
        graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);
        graphics.DrawMesh(Matrix4x4.Identity, doubleSided, Vector4.One);
        graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);
        graphics.EndScene();

        Received.InOrder(() =>
        {
            renderer.SetDepthTest(true);
            modelShader.Bind();
            renderer.SetFaceCulling(true);
            renderer.SetFaceCulling(false);
            emissiveShader.Bind();
            modelShader.Bind();
            renderer.SetFaceCulling(true);
            modelShader.Unbind();
        });
        renderer.Received(1).SetDepthTest(true);
        renderer.Received(2).SetFaceCulling(true);
        renderer.Received(1).SetFaceCulling(false);
        modelShader.Received(2).Bind();
        modelShader.Received(1).Unbind();
        emissiveShader.DidNotReceive().Unbind();
    }

    [Fact]
    public void DrawMesh_EmissiveMap_AddsTheMapAndLeavesOnlyTheExtraInTheLitPass()
    {
        var modelShader = Substitute.For<IShader>();
        var emissiveShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        shaderFactory.Create(ShaderId.Emissive).Returns(emissiveShader);

        var cube = InitializedMesh();
        cube.EmissiveFactor = new Vector3(1f, 1f, 1f);
        cube.EmissiveTexture = Substitute.For<Texture2D>();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var renderer = Substitute.For<IRendererAPI>();
        FakeUniformBuffer.Attach(renderer);
        var graphics = new Graphics3D(
            renderer, shaderFactory, meshFactory, Substitute.For<IFrameBufferFactory>());
        graphics.Init();
        var vp = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 4f), Vector3.Zero, Vector3.UnitY)
            * Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 1f, 0.1f, 20f);
        graphics.BeginScene(new SceneView(vp, new Vector3(0f, 0f, 4f)));
        graphics.ResetStats();

        graphics.DrawMesh(Matrix4x4.Identity, cube, Vector4.One, emissive: new Vector3(1.5f, 1f, 1f));

        modelShader.Received().SetFloat3("u_Emissive", new Vector3(0.5f, 0f, 0f));
        emissiveShader.Received().SetFloat3("u_Emissive", new Vector3(1f, 1f, 1f));
        emissiveShader.DidNotReceive().SetMat4("u_ViewProjection", Arg.Any<Matrix4x4>());
        renderer.Received().SetAdditiveBlend(true);
        renderer.Received().SetBlend(false);
        graphics.GetStats().DrawCalls.ShouldBe(2u);
    }

    [Fact]
    public void DrawMesh_RestoresUnitUvScaleAfterPaddedTexture()
    {
        var modelShader = Substitute.For<IShader>();
        var shaderFactory = Substitute.For<IShaderFactory>();
        shaderFactory.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        shaderFactory.Create(ShaderId.Model).Returns(modelShader);
        using var graphics = CreateGraphics3D(out var mesh, shaderFactory: shaderFactory);
        graphics.Init();
        graphics.BeginScene(new SceneView(Matrix4x4.Identity, Vector3.Zero));
        modelShader.ClearReceivedCalls();

        var scale = new Vector2(0.75f, 0.5f);
        using var paddedTexture = Substitute.For<Texture2D>();
        paddedTexture.UvScale.Returns(scale);
        mesh.DiffuseTexture = paddedTexture;
        graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);

        using var regularTexture = Substitute.For<Texture2D>();
        regularTexture.UvScale.Returns(Vector2.One);
        mesh.DiffuseTexture = regularTexture;
        graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);

        Received.InOrder(() =>
        {
            modelShader.SetFloat2("u_DiffuseUvScale", scale);
            modelShader.SetFloat2("u_DiffuseUvScale", Vector2.One);
        });
    }

    [Fact]
    public void BeginShadowPass_DrawCube_RecordsDirectionalShadowDraw()
    {
        var graphics = CreateGraphics3D(out _, withShadowMap: true);
        graphics.Init();
        graphics.ResetStats();

        graphics.BeginShadowPass(Matrix4x4.CreateOrthographic(2f, 2f, 0.1f, 10f));
        graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
        graphics.EndShadowPass();

        var stats = graphics.GetStats();
        stats.DrawCalls.ShouldBe(1u);
        stats.DirectionalShadowDrawCalls.ShouldBe(1u);
        stats.ColorDrawCalls.ShouldBe(0u);
        stats.CubeDraws.ShouldBe(1u);
    }

    private static Graphics3D CreateGraphics3D(
        out Mesh cube, bool withShadowMap = false, IShaderFactory? shaderFactory = null, IRendererAPI? renderer = null)
    {
        if (shaderFactory is null)
        {
            shaderFactory = Substitute.For<IShaderFactory>();
            shaderFactory.Create(Arg.Any<ShaderId>()).Returns(_ => Substitute.For<IShader>());
        }

        cube = InitializedMesh();
        var meshFactory = Substitute.For<IMeshFactory>();
        meshFactory.CreateCube().Returns(cube);

        var frameBuffers = Substitute.For<IFrameBufferFactory>();
        if (withShadowMap)
            frameBuffers.Create(Arg.Any<FrameBufferSpecification>()).Returns(Substitute.For<IFrameBuffer>());

        return new Graphics3D(
            renderer ?? FakeUniformBuffer.Renderer(),
            shaderFactory,
            meshFactory,
            frameBuffers);
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
        for (var i = 0; i < 8; i++)
            mesh.Vertices.Add(default);
        for (var i = 0; i < 36; i++)
            mesh.Indices.Add((uint)(i % 8));
        mesh.Initialize(vaoFactory, vboFactory, iboFactory);
        return mesh;
    }
}
