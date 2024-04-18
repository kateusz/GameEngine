using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.VertexArray;
using OpenTK.Graphics.OpenGL4;

namespace Engine.Platform.OpenTK;

public class SceneData
{
    public Matrix4x4 ViewProjectionMatrix { get; set; }
}

public class OpenTKRendererAPI : IRendererAPI
{
    public void Init()
    {
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        GL.Enable(EnableCap.DepthTest);
    }

    public void SetClearColor(Vector4 color)
        => GL.ClearColor(color.X, color.Y, color.Z, color.W);

    public void Clear()
        => GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

    public void DrawIndexed(IVertexArray vertexArray)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        GL.DrawElements(PrimitiveType.Triangles, indexBuffer.Count, DrawElementsType.UnsignedInt, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
    
    public int GetError()
    {
        return (int)GL.GetError();
    }
}