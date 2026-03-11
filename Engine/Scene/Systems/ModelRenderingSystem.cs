using System.Numerics;
using ECS;
using ECS.Systems;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;
using Serilog;

namespace Engine.Scene.Systems;

/// <summary>
/// System responsible for rendering 3D models with PBR materials, shadows, and multi-light support.
/// </summary>
internal sealed class ModelRenderingSystem(IGraphics3D graphics3D, IContext context) : ISystem
{
    private static readonly ILogger Logger = Log.ForContext<ModelRenderingSystem>();

    public int Priority => SystemPriorities.ModelRenderSystem;

    public void OnInit()
    {
        Logger.Debug("ModelRenderingSystem initialized with priority {Priority}", Priority);
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        Camera? mainCamera = null;
        var cameraTransform = Matrix4x4.Identity;

        var cameraGroup = context.View<CameraComponent>();
        foreach (var (entity, cameraComponent) in cameraGroup)
        {
            if (cameraComponent.Primary)
            {
                mainCamera = cameraComponent.Camera;
                var transformComponent = entity.GetComponent<TransformComponent>();
                cameraTransform = transformComponent.GetTransform();
                break;
            }
        }

        if (mainCamera == null)
            return;

        // Collect lights
        LightComponent? dirLight = null;
        var dirLightDirection = Vector3.UnitY * -1;
        var pointLights = new List<(Vector3 Position, LightComponent Light)>();
        var spotLights = new List<(Vector3 Position, LightComponent Light)>();

        var lightView = context.View<LightComponent>();
        foreach (var (entity, lightComponent) in lightView)
        {
            var lightTransform = entity.GetComponent<TransformComponent>();
            var lightPos = new Vector3(
                lightTransform.GetTransform().M41,
                lightTransform.GetTransform().M42,
                lightTransform.GetTransform().M43);

            switch (lightComponent.Type)
            {
                case LightType.Directional:
                    dirLight = lightComponent;
                    dirLightDirection = lightComponent.Direction;
                    break;
                case LightType.Point:
                    if (pointLights.Count < RenderingConstants.MaxPointLights)
                        pointLights.Add((lightPos, lightComponent));
                    break;
                case LightType.Spot:
                    if (spotLights.Count < RenderingConstants.MaxSpotLights)
                        spotLights.Add((lightPos, lightComponent));
                    break;
            }
        }

        // Shadow pass for directional light
        if (dirLight is { CastShadows: true })
        {
            var lightSpaceMatrix = Graphics3D.ComputeDirectionalLightSpaceMatrix(
                dirLightDirection, Vector3.Zero, 50.0f);

            graphics3D.BeginShadowPass(lightSpaceMatrix);

            var shadowView = context.View<MeshComponent>();
            foreach (var (entity, meshComponent) in shadowView)
            {
                var modelRenderer = entity.GetComponent<ModelRendererComponent>();
                if (modelRenderer is { CastShadows: true })
                {
                    var transformComponent = entity.GetComponent<TransformComponent>();
                    graphics3D.DrawShadowMesh(transformComponent.GetTransform(), meshComponent.Mesh);
                }
            }

            graphics3D.EndShadowPass();
        }

        // Main PBR pass
        graphics3D.BeginScene(mainCamera, cameraTransform);

        // Set collected lights
        if (graphics3D is Graphics3D pbrGraphics)
        {
            pbrGraphics.SetLights(
                dirLight, dirLightDirection,
                pointLights.ToArray().AsSpan(),
                spotLights.ToArray().AsSpan());
        }

        var view = context.View<MeshComponent>();
        foreach (var (entity, meshComponent) in view)
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            graphics3D.DrawModel(
                transformComponent.GetTransform(),
                meshComponent,
                modelRendererComponent,
                entity.Id
            );
        }

        graphics3D.EndScene();
    }

    public void OnShutdown() { }
}
