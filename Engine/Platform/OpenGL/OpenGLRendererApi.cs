using System.Numerics;
using Engine.Platform.SilkNet;
using Engine.Renderer;
using Engine.Renderer.VertexArray;
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

    public unsafe void DrawIndexed(IVertexArray vertexArray, uint count)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        var itemsCount = count != 0 ? count : (uint)indexBuffer.Count;

        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DrawElements");
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

    public void Init()
    {
        SilkNetContext.GL.Enable(EnableCap.Blend);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(Blend)");

        SilkNetContext.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BlendFunc");

        SilkNetContext.GL.Enable(EnableCap.DepthTest);
        OpenGLDebug.CheckError(SilkNetContext.GL, "Enable(DepthTest)");

        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        OpenGLDebug.CheckError(SilkNetContext.GL, "DepthFunc");
    }

    public int GetError()
    {
        return (int)SilkNetContext.GL.GetError();
    }
}