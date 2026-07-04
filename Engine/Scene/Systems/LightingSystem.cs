using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Scene;

namespace Engine.Scene.Systems;

internal sealed class LightingSystem(
    IGraphics3D graphics3D,
    IPrimaryCameraProvider cameraProvider,
    IContext context) : ISystem
{
    public int Priority => SystemPriorities.LightingSystem;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        SceneRenderPipeline.ApplyLighting(context, graphics3D);

        var camera = SceneRenderPipeline.CameraBinding.FromProvider(cameraProvider);
        if (!camera.IsValid)
            return;

        SceneRenderPipeline.RenderLightVisualization(context, graphics3D, camera);
    }

    public void OnShutdown() { }
}
