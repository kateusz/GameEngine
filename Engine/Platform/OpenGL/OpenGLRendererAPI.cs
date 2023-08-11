using Engine.Renderer;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Platform.OpenGL;

public class OpenGLRendererAPI : IRendererAPI
{
    public static void BeginScene()
    {
    }

    public static void EndScene()
    {
    }

    public static void Submit(IVertexArray vertexArray)
    {
        vertexArray.Bind();
        RendererCommand.DrawIndexed(vertexArray);
    }

    public ApiType ApiType { get; } = ApiType.OpenGL;

    public void SetClearColor(Vector4 color)
    {
        GL.ClearColor(color.X, color.Y, color.Z, color.W);
    }

    public void Clear()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit);
    }

    public void DrawIndexed(IVertexArray vertexArray)
    {
        var indexBuffer = vertexArray.IndexBuffer;
        GL.DrawElements(PrimitiveType.Triangles, indexBuffer.GetCount(), DrawElementsType.UnsignedInt, 0);

    }
}