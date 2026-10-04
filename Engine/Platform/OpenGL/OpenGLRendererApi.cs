using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLRendererApi : IRendererAPI
{
    private static readonly ILogger Logger = Log.ForContext<OpenGLRendererApi>();
    private static bool _brdfLutWarned;
    private readonly HashSet<uint> _meshInstanceLayoutVaos = [];
    public void SetClearColor(Vector4 color)
    {
        SilkNetContext.GL.ClearColor(color.X, color.Y, color.Z, color.W);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetClearColor");
    }

    public void Clear()
    {
        SilkNetContext.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Clear");
    }

    public void BindTexture2D(uint textureId, int slot = 0)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"ActiveTexture({slot})");
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, 0);
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, textureId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(Texture2D)");
    }

    public void BindTextureCube(uint textureId, int slot = 0)
    {
        SilkNetContext.GL.ActiveTexture(TextureUnit.Texture0 + slot);
        OpenGLDebug.CheckError(SilkNetContext.GL, $"ActiveTexture({slot})");
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, 0);
        SilkNetContext.GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindTexture(TextureCubeMap)");
    }

    public bool TryCreateSkyCapture(string absolutePath, out uint cubemapId, out ISkyCapture capture) =>
        OpenGLSkyCapture.TryCreate(absolutePath, out cubemapId, out capture);

    public bool TryCreateBrdfLut(out uint textureId)
    {
        textureId = 0;
        var gl = SilkNetContext.GL;
        var previousFbo = gl.GetInteger(GLEnum.FramebufferBinding);
        var viewport = new int[4];
        gl.GetInteger(GLEnum.Viewport, viewport);

        uint texture = 0, framebuffer = 0, vao = 0, vbo = 0;
        var kept = false;
        try
        {
            texture = gl.GenTexture();
            gl.BindTexture(TextureTarget.Texture2D, texture);
            unsafe
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.RG16f,
                    (uint)LightingMath.BrdfLutSize,
                    (uint)LightingMath.BrdfLutSize,
                    0,
                    PixelFormat.RG,
                    PixelType.Float,
                    (void*)0);
            }

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

            framebuffer = gl.GenFramebuffer();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);
            gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D,
                texture,
                0);
            OpenGLDebug.CheckError(gl, "BrdfLut create");
            if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            {
                WarnBrdfLutOnce("BRDF lookup framebuffer is incomplete");
                return false;
            }

            ReadOnlySpan<float> vertices =
            [
                -1f, -1f, 0f, 0f,
                1f, -1f, 1f, 0f,
                1f, 1f, 1f, 1f,
                -1f, -1f, 0f, 0f,
                1f, 1f, 1f, 1f,
                -1f, 1f, 0f, 1f
            ];
            vao = gl.GenVertexArray();
            vbo = gl.GenBuffer();
            gl.BindVertexArray(vao);
            gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
            unsafe
            {
                fixed (float* ptr = vertices)
                {
                    gl.BufferData(
                        BufferTargetARB.ArrayBuffer,
                        (nuint)(vertices.Length * sizeof(float)),
                        ptr,
                        BufferUsageARB.StaticDraw);
                }

                gl.EnableVertexAttribArray(0);
                gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
                gl.EnableVertexAttribArray(1);
                gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
            }

            gl.Viewport(0, 0, (uint)LightingMath.BrdfLutSize, (uint)LightingMath.BrdfLutSize);
            gl.Disable(EnableCap.Blend);
            gl.Clear(ClearBufferMask.ColorBufferBit);
            gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            OpenGLDebug.CheckError(gl, "BrdfLut draw");
            textureId = texture;
            kept = true;
            return true;
        }
        catch (Exception ex)
        {
            WarnBrdfLutOnce("BRDF lookup failed to create: " + ex.Message);
            return false;
        }
        finally
        {
            if (vbo != 0)
                gl.DeleteBuffer(vbo);
            if (vao != 0)
                gl.DeleteVertexArray(vao);
            if (framebuffer != 0)
                gl.DeleteFramebuffer(framebuffer);
            if (!kept && texture != 0)
                gl.DeleteTexture(texture);
            gl.Enable(EnableCap.Blend);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)previousFbo);
            gl.Viewport(viewport[0], viewport[1], (uint)viewport[2], (uint)viewport[3]);
            gl.BindVertexArray(0);
        }
    }

    private static void WarnBrdfLutOnce(string message)
    {
        if (_brdfLutWarned)
            return;
        _brdfLutWarned = true;
        Logger.Warning(message);
    }

    public void DeleteTexture(uint textureId)
    {
        if (textureId == 0)
            return;

        SilkNetContext.GL.DeleteTexture(textureId);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DeleteTexture");
    }

    public void BindDefaultFramebuffer()
    {
        SilkNetContext.GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BindFramebuffer(0)");
    }

    public void SetBoundTexture2DFilterLinear()
    {
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        OpenGLDebug.CheckError(SilkNetContext.GL, "TexParameter(Linear)");
    }

    public unsafe void DrawIndexed(IVertexArray vertexArray, uint count)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        var itemsCount = count != 0 ? count : (uint)indexBuffer.Count;

        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawElements");
    }

    public unsafe void DrawIndexedInstanced(IVertexArray vertexArray, uint indexCount, ReadOnlySpan<MeshInstanceData> instances)
    {
        if (instances.IsEmpty)
            return;

        var count = indexCount != 0 ? indexCount : (uint)vertexArray.IndexBuffer.Count;
        BindInstanceBuffer(vertexArray, instances);

        SilkNetContext.GL.DrawElementsInstanced(
            PrimitiveType.Triangles, count, DrawElementsType.UnsignedInt, (void*)0, (uint)instances.Length);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawElementsInstanced");
    }

    private uint _meshInstanceBuffer; // process lifetime; the GL context owns it until exit

    private unsafe void BindInstanceBuffer(IVertexArray vertexArray, ReadOnlySpan<MeshInstanceData> instances)
    {
        var gl = SilkNetContext.GL;
        if (_meshInstanceBuffer == 0)
            _meshInstanceBuffer = gl.GenBuffer();

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _meshInstanceBuffer);
        fixed (MeshInstanceData* data = instances)
        {
            gl.BufferData(
                BufferTargetARB.ArrayBuffer,
                (nuint)(instances.Length * sizeof(MeshInstanceData)),
                data,
                BufferUsageARB.DynamicDraw);
        }

        OpenGLDebug.CheckError(gl, "BufferData(mesh instances)");

        if (vertexArray is not OpenGLVertexArray oglVao || _meshInstanceLayoutVaos.Contains(oglVao.RendererId))
            return;

        vertexArray.Bind();
        var stride = (uint)sizeof(MeshInstanceData);
        // Locations 0–5 are the mesh. Instance layout is stored once per VAO.
        EnableMat4(gl, startLocation: 6, byteOffset: 0, stride);
        EnableMat4(gl, startLocation: 10, byteOffset: MeshInstanceData.NormalByteOffset, stride);
        EnableInt(gl, location: 14, byteOffset: MeshInstanceData.EntityIdByteOffset, stride);
        _meshInstanceLayoutVaos.Add(oglVao.RendererId);
    }

    private static unsafe void EnableMat4(GL gl, uint startLocation, int byteOffset, uint stride)
    {
        for (uint column = 0; column < 4; column++)
        {
            var location = startLocation + column;
            gl.EnableVertexAttribArray(location);
            gl.VertexAttribPointer(location, 4, VertexAttribPointerType.Float, false, stride, (void*)(byteOffset + column * 16));
            gl.VertexAttribDivisor(location, 1);
        }
    }

    private static unsafe void EnableInt(GL gl, uint location, int byteOffset, uint stride)
    {
        gl.EnableVertexAttribArray(location);
        gl.VertexAttribIPointer(location, 1, VertexAttribIType.Int, stride, (void*)byteOffset);
        gl.VertexAttribDivisor(location, 1);
    }

    public void DrawArrays(IVertexArray vertexArray, uint vertexCount)
    {
        vertexArray.Bind();
        SilkNetContext.GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawArrays(Triangles)");
    }

    public void DrawLines(IVertexArray vertexArray, uint vertexCount)
    {
        vertexArray.Bind();
        SilkNetContext.GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawArrays");
    }

    /// <summary>
    /// Sets line width
    /// </summary>
    /// <param name="width">Line Width Range: 1 to 1, otherwise will throw 1281 (GL_INVALID_VALUE) error</param>
    public void SetLineWidth(float width)
    {
        SilkNetContext.GL.LineWidth(width);
        OpenGLDebug.CheckError(SilkNetContext.GL, "LineWidth");
    }

    public void SetDepthTest(bool enabled)
    {
        if (enabled)
        {
            SilkNetContext.GL.Enable(EnableCap.DepthTest);
            // ImGui/2D leave GL_LESS; skybox writes z=w (ndc.z=1) and only passes with LEQUAL.
            SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        }
        else
            SilkNetContext.GL.Disable(EnableCap.DepthTest);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetDepthTest");
    }

    public void SetBlend(bool enabled)
    {
        if (enabled)
            SilkNetContext.GL.Enable(EnableCap.Blend);
        else
            SilkNetContext.GL.Disable(EnableCap.Blend);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetBlend");
    }

    public void SetFaceCulling(bool enabled)
    {
        if (enabled)
        {
            SilkNetContext.GL.Enable(EnableCap.CullFace);
            SilkNetContext.GL.CullFace(TriangleFace.Back);
        }
        else
            SilkNetContext.GL.Disable(EnableCap.CullFace);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetFaceCulling");
    }

    public void SetCullFrontFaces(bool cullFront)
    {
        SilkNetContext.GL.Enable(EnableCap.CullFace);
        SilkNetContext.GL.CullFace(cullFront ? TriangleFace.Front : TriangleFace.Back);
        OpenGLDebug.CheckError(SilkNetContext.GL, "CullFace");
    }

    public void SetDepthWrite(bool enabled)
    {
        SilkNetContext.GL.DepthMask(enabled);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetDepthWrite");
    }

    public void SetPolygonMode(Renderer.PolygonMode mode)
    {
        var glMode = mode switch
        {
            Engine.Renderer.PolygonMode.Line => Silk.NET.OpenGL.PolygonMode.Line,
            _ => Silk.NET.OpenGL.PolygonMode.Fill
        };
        SilkNetContext.GL.PolygonMode(TriangleFace.FrontAndBack, glMode);
        OpenGLDebug.CheckError(SilkNetContext.GL, "SetPolygonMode");
    }

    public void SetViewport(int x, int y, uint width, uint height)
    {
        SilkNetContext.GL.Viewport(x, y, width, height);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Viewport");
    }

    public void Init()
    {
        SilkNetContext.GL.Enable(EnableCap.Blend);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(Blend)");

        SilkNetContext.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BlendFunc");

        SilkNetContext.GL.Enable(EnableCap.DepthTest);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(DepthTest)");

        SilkNetContext.GL.Enable(EnableCap.CullFace);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(CullFace)");
        SilkNetContext.GL.CullFace(TriangleFace.Back);
        OpenGLDebug.CheckError(SilkNetContext.GL, "CullFace(Back)");

        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DepthFunc");

        SilkNetContext.GL.Enable(EnableCap.TextureCubeMapSeamless);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(TextureCubeMapSeamless)");
    }

    public int GetError()
    {
        return (int)SilkNetContext.GL.GetError();
    }
}