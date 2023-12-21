using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public static class RendererCommand
{
    private static readonly IRendererAPI RendererApi = RendererApiFactory.Create();

    public static void Init()
    {
        RendererApi.Init();
    }
    
    // todo: add viewport here

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
        var error = GL.GetError();
    }
}