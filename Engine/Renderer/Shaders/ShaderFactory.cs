using Engine.Platform.SilkNet;

namespace Engine.Renderer.Shaders;

public static class ShaderFactory
{
    public static IShader Create(string vertPath, string fragPath)
    {
        switch (RendererApiType.Type)
        {
            case ApiType.None:
                break;
            case ApiType.SilkNet:
                return new SilkNetShader(vertPath, fragPath);
            default:
                throw new ArgumentOutOfRangeException();
        }

        throw new ArgumentOutOfRangeException();
    }
}