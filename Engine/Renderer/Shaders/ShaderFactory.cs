using Engine.Platform.SilkNet;

namespace Engine.Renderer.Shaders;

public static class ShaderFactory
{
    public static IShader Create(string vertPath, string fragPath)
    {
        return RendererApiType.Type switch
        {
            ApiType.SilkNet => new SilkNetShader(vertPath, fragPath),
            _ => throw new NotSupportedException($"Unsupported Render API type: {RendererApiType.Type}")
        };
    }
}