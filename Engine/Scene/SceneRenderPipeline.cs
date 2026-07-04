using System.Numerics;
using Box2D.NetStandard.Dynamics.Bodies;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Lights;
using SceneComponents.Physics;
using SceneComponents.Rendering;

namespace Engine.Scene;

/// <summary>
/// Shared scene rendering used by ECS render systems and the editor viewport.
/// </summary>
internal static class SceneRenderPipeline
{
    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    internal readonly struct CameraBinding
    {
        public Camera? Camera { get; init; }
        public Matrix4x4 Transform { get; init; }
        public IViewCamera? ViewCamera { get; init; }

        public bool IsValid => ViewCamera != null || Camera != null;

        public static CameraBinding FromProvider(IPrimaryCameraProvider provider) =>
            new() { Camera = provider.Camera, Transform = provider.Transform };

        public static CameraBinding FromEditor(EditorCamera camera) =>
            new() { ViewCamera = camera };
    }

    public static void ApplyLighting(IContext context, IGraphics3D graphics3D)
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
    }

    public static void RenderLightVisualization(
        IContext context,
        IGraphics3D graphics3D,
        in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        var pointLights = context.View<PointLightComponent>().ToList();
        var directionalLights = context.View<DirectionalLightComponent>().ToList();

        Begin3DScene(graphics3D, camera, lightVisualization: true);
        foreach (var (entity, _) in pointLights)
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            graphics3D.DrawLightVisualization(transform.Translation);
        }

        foreach (var (entity, _) in directionalLights)
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            graphics3D.DrawLightVisualization(transform.Translation);
        }

        graphics3D.EndLightVisualization();
    }

    public static void RenderModels(IContext context, IGraphics3D graphics3D, in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin3DScene(graphics3D, camera);
        foreach (var (entity, meshComponent) in context.View<MeshComponent>())
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            var modelRendererComponent = entity.GetComponent<ModelRendererComponent>();

            graphics3D.DrawModel(
                transformComponent.GetTransform(),
                meshComponent,
                modelRendererComponent,
                entity.Id);
        }

        graphics3D.EndScene();
    }

    public static void RenderSprites(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin2DScene(graphics2D, camera);
        foreach (var (entity, spriteRendererComponent) in context.View<SpriteRendererComponent>())
        {
            var transformComponent = entity.GetComponent<TransformComponent>();
            Texture2D? texture = null;
            if (textureFactory != null && !string.IsNullOrWhiteSpace(spriteRendererComponent.TexturePath))
                texture = textureFactory.Create(PathBuilder.Build(spriteRendererComponent.TexturePath));

            if (texture is not null)
                graphics2D.DrawQuad(transformComponent.GetTransform(), texture, DefaultTextureCoords,
                    spriteRendererComponent.TilingFactor, spriteRendererComponent.Color, entity.Id);
            else
                graphics2D.DrawQuad(transformComponent.GetTransform(), spriteRendererComponent.Color, entity.Id);
        }

        graphics2D.EndScene();
    }

    public static void RenderSubTextures(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin2DScene(graphics2D, camera);
        foreach (var (entity, subtextureComponent) in context.View<SubTextureRendererComponent>())
        {
            if (textureFactory == null || string.IsNullOrWhiteSpace(subtextureComponent.TexturePath))
                continue;

            var texture = textureFactory.Create(PathBuilder.Build(subtextureComponent.TexturePath));
            if (texture == null)
                continue;

            var transform = entity.GetComponent<TransformComponent>().GetTransform();
            Vector2[] texCoords;
            if (subtextureComponent.TexCoords != null)
            {
                texCoords = subtextureComponent.TexCoords;
            }
            else
            {
                var subTexture = SubTexture2D.CreateFromCoords(
                    texture,
                    subtextureComponent.Coords,
                    subtextureComponent.CellSize,
                    subtextureComponent.SpriteSize);
                texCoords = subTexture.TexCoords;
            }

            graphics2D.DrawQuad(transform, texture, texCoords, 1.0f, Vector4.One, entity.Id);
        }

        graphics2D.EndScene();
    }

    public static void RenderPhysicsDebug(
        IContext context,
        IGraphics2D graphics2D,
        DebugSettings debugSettings,
        PhysicsRuntimeBodyStore bodyStore,
        in CameraBinding camera,
        bool useTransformFallbackWhenNoBody)
    {
        if (!debugSettings.ShowColliderBounds || !camera.IsValid)
            return;

        Begin2DScene(graphics2D, camera);
        foreach (var (entity, boxCollider) in context.View<BoxCollider2DComponent>())
        {
            if (bodyStore.TryGet(entity.Id, out var body))
                DrawColliderFromBody(graphics2D, entity, boxCollider, body);
            else if (useTransformFallbackWhenNoBody)
                DrawColliderFromTransform(graphics2D, entity, boxCollider);
        }

        graphics2D.EndScene();
    }

    public static void RenderScene(
        IContext context,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory? textureFactory,
        DebugSettings debugSettings,
        PhysicsRuntimeBodyStore bodyStore,
        in CameraBinding camera,
        bool useTransformFallbackWhenNoBody)
    {
        RenderSprites(context, graphics2D, textureFactory, camera);
        RenderSubTextures(context, graphics2D, textureFactory, camera);
        ApplyLighting(context, graphics3D);
        RenderModels(context, graphics3D, camera);
        RenderLightVisualization(context, graphics3D, camera);
        RenderPhysicsDebug(context, graphics2D, debugSettings, bodyStore, camera, useTransformFallbackWhenNoBody);
    }

    private static void Begin2DScene(IGraphics2D graphics2D, in CameraBinding camera)
    {
        if (camera.ViewCamera != null)
            graphics2D.BeginScene(camera.ViewCamera);
        else
            graphics2D.BeginScene(camera.Camera!, camera.Transform);
    }

    private static void Begin3DScene(IGraphics3D graphics3D, in CameraBinding camera, bool lightVisualization = false)
    {
        if (lightVisualization)
        {
            if (camera.ViewCamera != null)
                graphics3D.BeginLightVisualization(camera.ViewCamera);
            else
                graphics3D.BeginLightVisualization(camera.Camera!, camera.Transform);
            return;
        }

        if (camera.ViewCamera != null)
            graphics3D.BeginScene(camera.ViewCamera);
        else
            graphics3D.BeginScene(camera.Camera!, camera.Transform);
    }

    private static void DrawColliderFromBody(
        IGraphics2D graphics2D,
        Entity entity,
        BoxCollider2DComponent boxCollider,
        Body body)
    {
        var bodyPosition = body.GetPosition();
        var angle = body.GetAngle();
        var transform = entity.GetComponent<TransformComponent>();
        var color = GetRuntimeBodyDebugColor(body);
        var size = new Vector2(
            boxCollider.Size.X * 2.0f * transform.Scale.X,
            boxCollider.Size.Y * 2.0f * transform.Scale.Y);

        var offset = new Vector2(
            boxCollider.Offset.X * transform.Scale.X,
            boxCollider.Offset.Y * transform.Scale.Y);
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var rotatedOffset = new Vector2(
            offset.X * cos - offset.Y * sin,
            offset.X * sin + offset.Y * cos);
        var worldPos = new Vector3(
            bodyPosition.X + rotatedOffset.X,
            bodyPosition.Y + rotatedOffset.Y,
            0.0f);

        var trs = Matrix4x4.CreateTranslation(worldPos)
                  * Matrix4x4.CreateRotationZ(angle)
                  * Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
        graphics2D.DrawRect(trs, color, entity.Id);
    }

    private static void DrawColliderFromTransform(
        IGraphics2D graphics2D,
        Entity entity,
        BoxCollider2DComponent boxCollider)
    {
        var transform = entity.GetComponent<TransformComponent>();
        var size = new Vector2(
            boxCollider.Size.X * 2.0f * transform.Scale.X,
            boxCollider.Size.Y * 2.0f * transform.Scale.Y);
        var color = GetEditorColliderColor(entity);
        var rotation = transform.Rotation.Z;
        var cos = MathF.Cos(rotation);
        var sin = MathF.Sin(rotation);
        var scaledOffset = new Vector2(
            boxCollider.Offset.X * transform.Scale.X,
            boxCollider.Offset.Y * transform.Scale.Y);
        var rotatedOffset = new Vector2(
            scaledOffset.X * cos - scaledOffset.Y * sin,
            scaledOffset.X * sin + scaledOffset.Y * cos);
        var worldPos = new Vector3(
            transform.Translation.X + rotatedOffset.X,
            transform.Translation.Y + rotatedOffset.Y,
            0.0f);

        var trs = Matrix4x4.CreateTranslation(worldPos)
                  * Matrix4x4.CreateRotationZ(rotation)
                  * Matrix4x4.CreateScale(size.X, size.Y, 1.0f);
        graphics2D.DrawRect(trs, color, entity.Id);
    }

    private static Vector4 GetEditorColliderColor(Entity entity)
    {
        if (!entity.TryGetComponent<RigidBody2DComponent>(out var rb))
            return new Vector4(0.0f, 1.0f, 1.0f, 1.0f);

        return rb.BodyType switch
        {
            RigidBodyType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            RigidBodyType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
            _ => new Vector4(1.0f, 0.0f, 0.3f, 1.0f)
        };
    }

    private static Vector4 GetRuntimeBodyDebugColor(Body body)
    {
        if (!body.IsEnabled())
            return new Vector4(0.5f, 0.5f, 0.0f, 1.0f);

        return body.Type() switch
        {
            BodyType.Static => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
            BodyType.Kinematic => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
            _ => body.IsAwake()
                ? new Vector4(1.0f, 0.0f, 0.3f, 1.0f)
                : new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
        };
    }
}
