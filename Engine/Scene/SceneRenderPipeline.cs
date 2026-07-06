using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal static class SceneRenderPipeline
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SceneRenderPipeline));

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

    public static void RenderScene(
        IContext context,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory? textureFactory,
        in CameraBinding camera)
    {
        RenderSpritesAndSubTextures(context, graphics2D, textureFactory, camera);
        RenderCubes(context, graphics3D, camera);
    }

    private static void RenderCubes(IContext context, IGraphics3D graphics3D, in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin3DScene(graphics3D, camera);
        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        graphics3D.SetAmbientLight(ambientColor, ambientStrength);
        var (lightDirection, lightColor) = ResolveDirectional(context);
        graphics3D.SetDirectionalLight(lightDirection, lightColor);

        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            graphics3D.DrawCube(
                transformComponent.GetTransform(),
                modelRenderer.Color,
                entity.Id);
        }

        graphics3D.EndScene();
    }

    private static void RenderSpritesAndSubTextures(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin2DScene(graphics2D, camera);
        RenderSpritesInternal(context, graphics2D, textureFactory);
        RenderSubTexturesInternal(context, graphics2D, textureFactory);
        graphics2D.EndScene();
    }

    private static void RenderSpritesInternal(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory)
    {
        foreach (var (entity, spriteRendererComponent, transformComponent) in
                 context.View<SpriteRendererComponent, TransformComponent>())
        {
            var transform = transformComponent.GetTransform();
            if (!string.IsNullOrWhiteSpace(spriteRendererComponent.TexturePath) && textureFactory != null)
            {
                try
                {
                    var texture = textureFactory.Create(PathBuilder.Resolve(spriteRendererComponent.TexturePath));
                    graphics2D.DrawQuad(transform, texture, DefaultTextureCoords, spriteRendererComponent.TilingFactor,
                        spriteRendererComponent.Color, entity.Id);
                    continue;
                }
                catch (Exception ex)
                {
                    Logger.Warning(
                        ex,
                        "Failed to load sprite texture '{TexturePath}' — drawing a solid color quad instead",
                        spriteRendererComponent.TexturePath);
                }
            }

            graphics2D.DrawQuad(transform, spriteRendererComponent.Color, entity.Id);
        }
    }

    private static void RenderSubTexturesInternal(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory)
    {
        foreach (var (entity, subtextureComponent, transformComponent) in
                 context.View<SubTextureRendererComponent, TransformComponent>())
        {
            if (textureFactory == null || string.IsNullOrWhiteSpace(subtextureComponent.TexturePath))
                continue;

            var texture = textureFactory.Create(PathBuilder.Resolve(subtextureComponent.TexturePath));
            var transform = transformComponent.GetTransform();
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
    }

    private static void Begin2DScene(IGraphics2D graphics2D, in CameraBinding camera)
    {
        if (camera.ViewCamera != null)
            graphics2D.BeginScene(camera.ViewCamera);
        else
            graphics2D.BeginScene(camera.Camera!, camera.Transform);
    }

    private static void Begin3DScene(IGraphics3D graphics3D, in CameraBinding camera)
    {
        if (camera.ViewCamera != null)
            graphics3D.BeginScene(camera.ViewCamera);
        else
            graphics3D.BeginScene(camera.Camera!, camera.Transform);
    }

    private static (Vector3 Color, float Strength) ResolveAmbient(IContext context)
    {
        foreach (var (_, alc) in context.View<AmbientLightComponent>())
            return (alc.Color, alc.Strength);

        return (Vector3.One, 0.1f);
    }

    private static (Vector3 Direction, Vector3 Color) ResolveDirectional(IContext context)
    {
        foreach (var (_, dlc) in context.View<DirectionalLightComponent>())
            return (NormalizeDirection(dlc.Direction), dlc.Color);

        return (new Vector3(0, -1, 0), Vector3.Zero);
    }

    private static Vector3 NormalizeDirection(Vector3 direction) =>
        direction.LengthSquared() < 1e-6f ? new Vector3(0, -1, 0) : Vector3.Normalize(direction);
}
