using Engine.Platform.OpenGL;

namespace Engine.Renderer;

public static class ShaderFactory
{
    public static IShader Create(string vertPath, string fragPath)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.OpenGL:
                return new OpenGLShader(vertPath, fragPath);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}