namespace Engine.Renderer;

public static class RendererSingleton
{
    public static readonly IRendererAPI Instance = RendererApiFactory.Create();
}