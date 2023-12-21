using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Platform.OpenGL;

public class SceneData
{
    public Matrix4 ViewProjectionMatrix { get; set; }
}

public class OpenGLRendererAPI : IRendererAPI
{
    public ApiType ApiType { get; } = ApiType.OpenGL;

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
        GL.DrawElements(PrimitiveType.Triangles, indexBuffer.GetCount(), DrawElementsType.UnsignedInt, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}