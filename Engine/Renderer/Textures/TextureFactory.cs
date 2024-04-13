using Engine.Platform.OpenGL;
using Engine.Platform.SilkNet;

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
            case ApiType.OpenTK:
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
            case ApiType.OpenTK:
                return OpenGLTexture2D.Create(path);
            case ApiType.SilkNet:
                return SilkNetTexture2D.Create(path);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}