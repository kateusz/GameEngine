using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures;

public static class TextureFactory
{
    [Obsolete("Use overload with path parameter")]
    public static Texture2D Create(int width, int height)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return OpenGLTexture2D.Create(width, height);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
    
    public static Texture2D Create(string path)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return OpenGLTexture2D.Create(path);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}