using Engine.Platform.OpenGL;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public static class RendererCommand
{
    private static readonly IRendererAPI RendererApi = new OpenGLRendererAPI();

    public static void DrawIndexed(IVertexArray vertexArray)
    {
        RendererApi.DrawIndexed(vertexArray);
    }

    public static void SetClearColor(Vector4 color)
    {
        RendererApi.SetClearColor(color);
    }

    public static void Clear()
    {
        RendererApi.Clear();
    }
}