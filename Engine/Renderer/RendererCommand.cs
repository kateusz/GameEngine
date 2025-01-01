using Engine.Renderer.VertexArray;
using System.Numerics;

namespace Engine.Renderer;

public static class RendererCommand
{
    private static readonly IRendererAPI RendererApi = RendererApiFactory.Create();

    public static void Init()
    {
        RendererApi.Init();
    }
    
    public static void DrawIndexed(IVertexArray vertexArray, uint count = 0)
    {
        RendererApi.DrawIndexed(vertexArray, count);
    }
    
    public static void DrawLines(IVertexArray vertexArray, uint vertexCount)
    {
        RendererApi.DrawLines(vertexArray, vertexCount);
    }

    public static void SetLineWidth(float width)
    {
        RendererApi.SetLineWidth(width);
    }

    public static void SetClearColor(Vector4 color)
    {
        RendererApi.SetClearColor(color);
    }

    public static void Clear()
    {
        RendererApi.Clear();
        var errorCode = RendererApi.GetError();
        if (errorCode != 0)
        {
            throw new Exception($"Rendering error! Error code: {errorCode}");
        }
    }
}