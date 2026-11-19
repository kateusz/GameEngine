using System.Numerics;
using Engine.Core.DI;
using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using Engine.Scene.Cameras;
using Shouldly;
using Silk.NET.OpenGL;

namespace Engine.GraphicsTests;

[Collection("GraphicsIntegration")]
public class RenderStateContractTests(HeadlessGraphicsContextFixture fixture)
    : IClassFixture<HeadlessGraphicsContextFixture>
{
    [GraphicsFact]
    public void AllocateResizeAndFailure_PreserveSeparateTargetsViewportAndTextures()
    {
        var gl = SilkNetContext.GL;
        using var draw = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(32, 32));
        using var read = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(32, 32));
        draw.Bind();
        var drawId = gl.GetInteger(GLEnum.DrawFramebufferBinding);
        read.Bind();
        var readId = gl.GetInteger(GLEnum.ReadFramebufferBinding);
        read.Unbind();
        gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, (uint)readId);
        gl.Viewport(3, 4, 17, 19);
        using var textures = new TextureFactory(new EngineHostOptions("test", 1, 1));
        using var texture = textures.Create(2, 2);
        texture.Bind(5);
        var textureId = gl.GetInteger(GLEnum.TextureBinding2D);
        try
        {
            using var other = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(8, 8));
            AssertPreserved();
            other.Resize(16, 16);
            AssertPreserved();
            Should.Throw<NotSupportedException>(() => fixture.FrameBufferFactory.Create(new(8, 8)
            {
                AttachmentsSpec = new([new(FrameBufferTextureFormat.None)])
            }));
            AssertPreserved();
        }
        finally { draw.Unbind(); gl.ActiveTexture(TextureUnit.Texture0); }

        void AssertPreserved()
        {
            gl.GetInteger(GLEnum.DrawFramebufferBinding).ShouldBe(drawId);
            gl.GetInteger(GLEnum.ReadFramebufferBinding).ShouldBe(readId);
            var viewport = new int[4];
            gl.GetInteger(GLEnum.Viewport, viewport);
            viewport.ShouldBe(new[] { 3, 4, 17, 19 });
            gl.GetInteger(GLEnum.ActiveTexture).ShouldBe((int)TextureUnit.Texture5);
            gl.GetInteger(GLEnum.TextureBinding2D).ShouldBe(textureId);
        }
    }

    [GraphicsFact]
    public void TargetScope_RestoresOnException_AndResizingBoundTargetKeepsItsNewAttachments()
    {
        var gl = SilkNetContext.GL;
        using var outer = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(32, 32));
        using var inner = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(8, 8));
        using var outerBinding = outer.BindScoped();
        var outerId = gl.GetInteger(GLEnum.DrawFramebufferBinding);
        Should.Throw<InvalidOperationException>(() =>
        {
            using var innerBinding = inner.BindScoped();
            inner.Resize(16, 16);
            gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer).ShouldBe(GLEnum.FramebufferComplete);
            inner.ClearAttachment(1, 73);
            inner.ReadPixel(1, 8, 8).ShouldBe(73);
            throw new InvalidOperationException("simulated draw failure");
        });
        gl.GetInteger(GLEnum.DrawFramebufferBinding).ShouldBe(outerId);
        gl.GetInteger(GLEnum.ReadFramebufferBinding).ShouldBe(outerId);
        var viewport = new int[4];
        gl.GetInteger(GLEnum.Viewport, viewport);
        viewport.ShouldBe(new[] { 0, 0, 32, 32 });
        Should.Throw<InvalidOperationException>(() => outer.Bind());
    }

    [GraphicsFact]
    public void ColdShadowMapsAndQualityChange_ReturnToHdr_AndPointCullingStaysFront()
    {
        var gl = SilkNetContext.GL;
        using var textures = new TextureFactory(new EngineHostOptions("test", 1, 1));
        using var meshes = new MeshFactory(textures, fixture.VertexArrayFactory, fixture.VertexBufferFactory, fixture.IndexBufferFactory);
        using var graphics = new Graphics3D(fixture.RendererApi, fixture.ShaderFactory, meshes, fixture.FrameBufferFactory);
        graphics.Init();
        using var target = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(32, 32));
        target.Bind();
        var targetId = gl.GetInteger(GLEnum.DrawFramebufferBinding);
        try
        {
            for (var i = 0; i < 2; i++)
            {
                graphics.SetShadowQuality(new ShadowQuality(DirectionalResolution: i == 0 ? 1024 : 2048));
                graphics.BeginShadowPass(Matrix4x4.Identity);
                try { graphics.DrawCube(Matrix4x4.Identity, Vector4.One); }
                finally { graphics.EndShadowPass(); }
                gl.GetInteger(GLEnum.DrawFramebufferBinding).ShouldBe(targetId);
            }
            for (var entity = 1; entity <= 2; entity++)
            {
                graphics.BeginPointShadowFace(0, entity, 0, Matrix4x4.Identity, new(0, 0, 3), 10).ShouldBeTrue();
                try
                {
                    graphics.DrawCube(Matrix4x4.Identity, Vector4.One);
                    gl.GetInteger(GLEnum.CullFaceMode).ShouldBe((int)TriangleFace.Front);
                    var mesh = meshes.CreateCube();
                    mesh.DoubleSided = true;
                    graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);
                    gl.IsEnabled(EnableCap.CullFace).ShouldBeFalse();
                    mesh.DoubleSided = false;
                    graphics.DrawMesh(Matrix4x4.Identity, mesh, Vector4.One);
                    gl.IsEnabled(EnableCap.CullFace).ShouldBeTrue();
                    gl.GetInteger(GLEnum.CullFaceMode).ShouldBe((int)TriangleFace.Front);
                }
                finally { graphics.EndPointShadowFace(); }
                gl.GetInteger(GLEnum.DrawFramebufferBinding).ShouldBe(targetId);
                gl.GetInteger(GLEnum.CullFaceMode).ShouldBe((int)TriangleFace.Back);
            }
        }
        finally { target.Unbind(); }
    }

    [GraphicsFact]
    public void Instancing_RecreatedVaosAndDifferentRendererBuffers_KeepAttributesAndPicking()
    {
        var gl = SilkNetContext.GL;
        // Two API instances deliberately overwrite the same VAO's instance-buffer binding.
        // This is deterministic even on drivers that do not immediately recycle VAO names.
        var first = new OpenGLRendererApi();
        var second = new OpenGLRendererApi();
        using var textures = new TextureFactory(new EngineHostOptions("test", 1, 1));
        using var meshes = new MeshFactory(textures, fixture.VertexArrayFactory, fixture.VertexBufferFactory, fixture.IndexBufferFactory);
        using var graphics = new Graphics3D(first, fixture.ShaderFactory, meshes, fixture.FrameBufferFactory);
        graphics.Init();
        using var target = fixture.FrameBufferFactory.Create(FrameBufferFactory.SceneTarget(32, 32));
        var camera = new SceneCamera();
        camera.SetOrthographic(2, -10, 10);
        camera.SetViewportSize(32, 32);
        var view = CameraViews.From(camera, Matrix4x4.CreateTranslation(0, 0, 3));
        for (var generation = 0; generation < 6; generation++)
        {
            using var mesh = new Mesh("recreated");
            mesh.Vertices.AddRange(MeshFactory.CubeTemplate.Vertices);
            mesh.Indices.AddRange(MeshFactory.CubeTemplate.Indices);
            mesh.Initialize(fixture.VertexArrayFactory, fixture.VertexBufferFactory, fixture.IndexBufferFactory);
            MeshInstanceData[] data = [Graphics3D.PackInstance(new MeshDrawInstance
                { Transform = Matrix4x4.Identity, EntityId = 41, Tint = Vector4.One })];
            target.Bind();
            try
            {
                graphics.SetClearColor(Vector4.Zero);
                graphics.Clear();
                target.ClearAttachment(1, -1);
                graphics.BeginScene(view);
                try
                {
                    var vao = mesh.GetVertexArray();
                    vao.Bind();
                    graphics.DrawMeshInstances(mesh, [
                        new MeshDrawInstance { Transform = Matrix4x4.Identity, EntityId = 41, Tint = Vector4.One },
                        new MeshDrawInstance { Transform = Matrix4x4.CreateTranslation(20, 0, 0), EntityId = 42, Tint = Vector4.One }
                    ]);
                    var expected = AttributeBuffer();
                    second.DrawIndexedInstanced(vao, (uint)mesh.GetIndexCount(), data);
                    AttributeBuffer().ShouldNotBe(expected);
                    graphics.Clear();
                    target.ClearAttachment(1, -1);
                    graphics.DrawMeshInstances(mesh, [
                        new MeshDrawInstance { Transform = Matrix4x4.Identity, EntityId = 41, Tint = Vector4.One, Roughness = 0.5f },
                        new MeshDrawInstance { Transform = Matrix4x4.CreateTranslation(20, 0, 0), EntityId = 42, Tint = Vector4.One }
                    ]);
                    AttributeBuffer().ShouldBe(expected);
                    target.ReadPixel(1, 16, 16).ShouldBe(41);
                }
                finally { graphics.EndScene(); }
            }
            finally { target.Unbind(); }
        }
        unsafe int AttributeBuffer()
        {
            int buffer;
            gl.GetVertexAttrib(6, GLEnum.VertexAttribArrayBufferBinding, &buffer);
            return buffer;
        }
    }
}
