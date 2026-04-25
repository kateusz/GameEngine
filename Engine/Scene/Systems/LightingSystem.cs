using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Scene.Components;
using Engine.Scene.Components.Lights;

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
        var pointLights = context.View<PointLightComponent>().ToList();
        var directionalLights = context.View<DirectionalLightComponent>().ToList();
        var ambientLights = context.View<AmbientLightComponent>().ToList();

        if (directionalLights.Count > 0)
        {
            var (_, directionalLight) = directionalLights[0];
            graphics3D.SetDirectionalLight(
                enabled: true,
                direction: directionalLight.Direction,
                color: directionalLight.Color,
                strength: directionalLight.Strength);
        }
        else
        {
            graphics3D.SetDirectionalLight(
                enabled: false,
                direction: default,
                color: default,
                strength: 0.0f);
        }

        if (ambientLights.Count > 0)
        {
            var (_, ambientLight) = ambientLights[0];
            graphics3D.SetAmbientLight(
                enabled: true,
                color: ambientLight.Color,
                strength: ambientLight.Strength);
        }
        else
        {
            graphics3D.SetAmbientLight(
                enabled: false,
                color: default,
                strength: 0.0f);
        }

        var pointLightData = new List<PointLightData>(16);
        foreach (var (entity, pointLight) in pointLights)
        {
            if (pointLightData.Count >= 16)
                break;
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            pointLightData.Add(new PointLightData(
                transform.Translation,
                pointLight.Color,
                pointLight.Intensity));
        }
        graphics3D.SetPointLights(pointLightData);

        if (cameraProvider.Camera == null)
            return;

        graphics3D.BeginLightVisualization(cameraProvider.Camera, cameraProvider.Transform);
        foreach (var (e, _) in pointLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var worldPos = transform.Translation;
            graphics3D.DrawLightVisualization(worldPos);
        }

        foreach (var (e, _) in directionalLights)
        {
            if (!e.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var worldPos = transform.Translation;
            graphics3D.DrawLightVisualization(worldPos);
        }
        graphics3D.EndLightVisualization();
    }

    public void OnShutdown() { }
}
