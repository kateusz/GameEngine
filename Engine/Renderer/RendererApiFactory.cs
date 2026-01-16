using Engine.Platform.OpenGL;

namespace Engine.Renderer;

internal sealed class RendererApiFactory(IRendererApiConfig apiConfig) : IRendererApiFactory
{
    public IRendererAPI Create()
    {
        return apiConfig.Type switch
        {
            ApiType.OpenGL => new OpenGLRendererApi(),
            _ => throw new NotSupportedException($"Unsupported Render API type: {apiConfig.Type}")
        };
    }
}
