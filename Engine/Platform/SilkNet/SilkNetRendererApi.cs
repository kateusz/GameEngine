using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetRendererApi : IRendererAPI
{
    public void SetClearColor(Vector4 color)
    {
        SilkNetContext.GL.ClearColor(color.X, color.Y, color.Z, color.W);
    }

    public void Clear()
    {
        SilkNetContext.GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    public unsafe void DrawIndexed(IVertexArray vertexArray, uint count)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        var itemsCount = count != 0 ? count : (uint)indexBuffer.Count;

        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0); // check with: IntPtr.Zero);
    }

    public void DrawLines(IVertexArray vertexArray, uint vertexCount)
    {
        vertexArray.Bind();
        SilkNetContext.GL.DrawArrays(PrimitiveType.Lines, 0, vertexCount);
    }

    /// <summary>
    /// Sets line width
    /// </summary>
    /// <param name="width">Line Width Range: 1 to 1, otherwise will throw 1281 (GL_INVALID_VALUE) error</param>
    public void SetLineWidth(float width)
    {
        SilkNetContext.GL.LineWidth(width);
    }

    public void Init()
    {
        SilkNetContext.GL.Enable(EnableCap.Blend);
        SilkNetContext.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        SilkNetContext.GL.Enable(EnableCap.DepthTest);
        SilkNetContext.GL.DepthFunc(DepthFunction.Lequal); // or another appropriate function
    }

    public int GetError()
    {
        return (int)SilkNetContext.GL.GetError();
    }
}