using ECS;
using ECS.Systems;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;

namespace Engine.Scene.Systems;

internal sealed class SceneRenderSystem(
    IGraphics2D graphics2D,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    Context context,
    IModelFactory modelFactory) : ISystem
{
    public int Priority => 150;

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!CameraQueries.TryGetPrimaryView(context, out var view))
            return;

        SceneRenderPipeline.RenderScene(
            context, graphics2D, graphics3D, textureFactory, modelFactory,
            view);
    }
}
