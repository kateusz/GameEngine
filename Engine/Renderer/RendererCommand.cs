using Engine.Platform.OpenGL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Engine.Renderer;

public static class RendererCommand
{
    public static void Init()
    {
        RendererSingleton.Instance.Init();
    }
    
    public static void DrawIndexed(IVertexArray vertexArray)
    {
        RendererSingleton.Instance.DrawIndexed(vertexArray);
    }

    public static void SetClearColor(Vector4 color)
    {
        RendererSingleton.Instance.SetClearColor(color);
    }

    public static void Clear()
    {
        RendererSingleton.Instance.Clear();
        var error = GL.GetError();
    }
}