using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.VertexArray;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

public class SilkNetRendererAPI : IRendererAPI
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
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void DrawLines(IVertexArray vertexArray, uint vertexCount)
    {
        throw new NotImplementedException();
    }

    public void SetLineWidth(float width)
    {
        throw new NotImplementedException();
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