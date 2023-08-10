using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class IndexBufferFactory
{
    public static IIndexBuffer Create(uint[] indices, int count)
    {
        switch (Renderer.RendererApi)
        {
            case RendererApi.None:
                break;
            case RendererApi.OpenGL:
                return new OpenGLIndexBuffer(indices, count);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}