using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Scene.Components;

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
        var lights = context.View<LightingComponent>().ToList();
        if (lights.Count == 0)
            return;

        var (_, firstLight) = lights[0];
        graphics3D.SetLightPosition(firstLight.Position);
        graphics3D.SetLightDirection(firstLight.Direction);
        graphics3D.SetLightType((int)firstLight.Type);
        graphics3D.SetLightColor(firstLight.Color);

        if (cameraProvider.Camera == null)
            return;

        graphics3D.BeginLightVisualization(cameraProvider.Camera, cameraProvider.Transform);
        foreach (var (_, lightingComponent) in lights)
        {
            var worldPos = lightingComponent.Position;
            graphics3D.DrawLightVisualization(worldPos);
        }
        graphics3D.EndLightVisualization();
    }

    public void OnShutdown() { }
}
