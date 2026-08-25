using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.Buffers.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.OpenGL;

internal sealed class OpenGLRendererApi : IRendererAPI
{
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

    public unsafe void DrawIndexed(IVertexArray vertexArray, uint count)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        var itemsCount = count != 0 ? count : (uint)indexBuffer.Count;

        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawElements");
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