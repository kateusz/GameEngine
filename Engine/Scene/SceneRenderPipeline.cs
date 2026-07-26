using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Cameras;
using Engine.Renderer.Textures;
using Engine.Scene.Systems;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal static class SceneRenderPipeline
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SceneRenderPipeline));
    // ponytail: debug — once-per-entity tint warnings; clear not needed (reload editor resets process)
    private static readonly HashSet<int> WarnedTintEntities = [];

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
        IModelFactory modelFactory,
        in CameraBinding camera)
    {
        RenderSpritesAndSubTextures(context, graphics2D, textureFactory, camera);
        RenderModels(context, graphics3D, modelFactory, camera);
    }

    private static void RenderModels(IContext context, IGraphics3D graphics3D, IModelFactory modelFactory, in CameraBinding camera)
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
            var transform = transformComponent.GetWorldTransform();
            var tint = modelRenderer.Color;

            if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
            {
                graphics3D.DrawCube(transform, tint, entity.Id);
                continue;
            }

            var resolvedPath = PathBuilder.Resolve(modelRenderer.ModelPath);
            var model = modelFactory.Create(resolvedPath);
            if (model == null)
            {
                Logger.Warning(
                    "Failed to load model assetPath={ModelPath} resolved={ResolvedPath} — drawing unit cube instead",
                    modelRenderer.ModelPath, resolvedPath);
                graphics3D.DrawCube(transform, tint, entity.Id);
                continue;
            }

            // ponytail: debug tint/alpha once per entity — Color.a==0 → fully invisible; Color.rgb==0 → black
            if (WarnedTintEntities.Add(entity.Id))
            {
                if (tint.W <= 0f)
                {
                    Logger.Warning(
                        "ModelRenderer Color.a={Alpha} makes mesh invisible entity={EntityId} path={Path} color={Color}",
                        tint.W, entity.Id, modelRenderer.ModelPath, tint);
                }
                else if (tint.X <= 0f && tint.Y <= 0f && tint.Z <= 0f)
                {
                    Logger.Warning(
                        "ModelRenderer Color.rgb is black entity={EntityId} path={Path} color={Color} — mesh draws but looks invisible",
                        entity.Id, modelRenderer.ModelPath, tint);
                }
                else
                {
                    Logger.Information(
                        "ModelRenderer draw entity={EntityId} path={Path} tint={Color} hasAlbedo={HasAlbedo} submeshes={Count}",
                        entity.Id, modelRenderer.ModelPath, tint,
                        model.Submeshes[0].Material.HasAlbedoMap, model.Submeshes.Count);
                }
            }

            foreach (var submesh in model.Submeshes)
            {
                var metallic = modelRenderer.MetallicOverride ?? submesh.Material.Metallic;
                var roughness = modelRenderer.RoughnessOverride ?? submesh.Material.Roughness;
                graphics3D.DrawMesh(transform, submesh.Mesh, submesh.Material, tint, metallic, roughness, entity.Id);
            }
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
            if (spriteRendererComponent.Color.W <= 0f)
                continue;

            var transform = transformComponent.GetWorldTransform();
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
            var transform = transformComponent.GetWorldTransform();
            var texCoords = subtextureComponent.TexCoords ?? SubTexture2D.CreateFromCoords(
                texture,
                subtextureComponent.Coords,
                subtextureComponent.CellSize,
                subtextureComponent.SpriteSize).TexCoords;

            graphics2D.DrawQuad(transform, texture, texCoords, 1.0f, Vector4.One, entity.Id);
        }
    }

    internal static void Begin2DScene(IGraphics2D graphics2D, in CameraBinding camera)
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
        {
            var direction = dlc.Direction.LengthSquared() < 1e-6f
                ? new Vector3(0, -1, 0)
                : Vector3.Normalize(dlc.Direction);
            return (direction, dlc.Color);
        }

        // Metals get zero ambient in the PBR shader; without a directional light they are pure black.
        return (new Vector3(0, -1, 0), Vector3.One);
    }
}
