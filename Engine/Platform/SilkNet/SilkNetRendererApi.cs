using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

internal sealed class SilkNetRendererApi : IRendererAPI
{
    public void SetClearColor(Vector4 color)
    {
        SilkNetContext.GL.ClearColor(color.X, color.Y, color.Z, color.W);
        GLDebug.CheckError(SilkNetContext.GL, "SetClearColor");
    }

    public void Clear()
    {
        SilkNetContext.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GLDebug.CheckError(SilkNetContext.GL, "Clear");
    }

    public unsafe void DrawIndexed(IVertexArray vertexArray, uint count)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        var itemsCount = count != 0 ? count : (uint)indexBuffer.Count;

        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0);
        GLDebug.CheckError(SilkNetContext.GL, "DrawElements");
    }

    public void DrawLines(IVertexArray vertexArray, uint vertexCount)
    {
        vertexArray.Bind();
        SilkNetContext.GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
        GLDebug.CheckError(SilkNetContext.GL, "DrawArrays");
    }

    /// <summary>
    /// Sets line width
    /// </summary>
    /// <param name="width">Line Width Range: 1 to 1, otherwise will throw 1281 (GL_INVALID_VALUE) error</param>
    public void SetLineWidth(float width)
    {
        SilkNetContext.GL.LineWidth(width);
        GLDebug.CheckError(SilkNetContext.GL, "LineWidth");
    }

    public void Init()
    {
        SilkNetContext.GL.Enable(EnableCap.Blend);
        GLDebug.CheckError(SilkNetContext.GL, "Enable(Blend)");

        SilkNetContext.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GLDebug.CheckError(SilkNetContext.GL, "BlendFunc");

        SilkNetContext.GL.Enable(EnableCap.DepthTest);
        GLDebug.CheckError(SilkNetContext.GL, "Enable(DepthTest)");

        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal);
        GLDebug.CheckError(SilkNetContext.GL, "DepthFunc");
    }

    public int GetError()
    {
        return (int)SilkNetContext.GL.GetError();
    }
}