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

    public unsafe void DrawIndexed(IVertexArray vertexArray)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        SilkNetContext.GL.DrawElements(PrimitiveType.Triangles, (uint)indexBuffer.Count,
            DrawElementsType.UnsignedInt, (void*)0);
        SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Init()
    {
        SilkNetContext.GL.Enable(EnableCap.Blend);
        SilkNetContext.GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        SilkNetContext.GL.Enable(EnableCap.DepthTest);
    }

    public int GetError()
    {
        return (int)SilkNetContext.GL.GetError();
    }
}