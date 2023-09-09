using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class TextureFactory
{
    public static async Task<Texture2D> Create(string path)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return await OpenGLTexture2D.Create(path);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}