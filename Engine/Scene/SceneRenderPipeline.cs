using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
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
    private const string BuiltinSphereModelPath = "builtin:sphere";
    private static readonly HashSet<int> WarnedTintEntities = [];

    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    private enum ModelDrawKind
    {
        Cube,
        BuiltinSphere,
        Mesh
    }

    private readonly record struct ModelDrawItem(
        ModelDrawKind Kind,
        Matrix4x4 Transform,
        Vector3 WorldPosition,
        Mesh? Mesh,
        MeshMaterial? Material,
        Vector4 Tint,
        float Metallic,
        float Roughness,
        int EntityId,
        Matrix4x4[]? BonePalette = null,
        Texture2D? AlbedoOverride = null);

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
        RenderModels(context, graphics3D, modelFactory, textureFactory, camera);
    }

    private static void RenderModels(
        IContext context, IGraphics3D graphics3D, IModelFactory modelFactory, ITextureFactory? textureFactory, in CameraBinding camera)
    {
        if (!camera.IsValid)
            return;

        Begin3DScene(graphics3D, camera);
        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        graphics3D.SetAmbientLight(ambientColor, ambientStrength);
        var (lightDirection, lightColor, lightStrength) = ResolveDirectional(context);
        graphics3D.SetDirectionalLight(lightDirection, lightColor, lightStrength);
        var (skyLightPath, skyLightIntensity) = ResolveSkyLight(context);
        graphics3D.SetEnvironment(skyLightPath, skyLightIntensity);

        if (graphics3D.BeginShadowPass())
        {
            DrawOpaqueModels(context, graphics3D, modelFactory, textureFactory);
            graphics3D.EndShadowPass();
        }

        var (pointPosition, pointColor, pointStrength, pointRange) = ResolvePoint(context);
        graphics3D.SetPointLight(pointPosition, pointColor, pointStrength, pointRange);
        if (graphics3D.BeginPointShadowPass())
        {
            for (var face = 0; face < 6; face++)
            {
                graphics3D.SetPointShadowFace(face);
                DrawOpaqueModels(context, graphics3D, modelFactory, textureFactory);
            }
            graphics3D.EndPointShadowPass();
        }

        graphics3D.DrawSkybox();
        DrawOpaqueModels(context, graphics3D, modelFactory, textureFactory);
        DrawTransparentModels(context, graphics3D, modelFactory, textureFactory, GetCameraPosition(camera));

        graphics3D.EndScene();
    }

    private static void DrawOpaqueModels(
        IContext context, IGraphics3D graphics3D, IModelFactory modelFactory, ITextureFactory? textureFactory)
    {
        foreach (var item in EnumerateModelDrawItems(
                     context, modelFactory, textureFactory, static mode => mode != MaterialAlphaMode.Blend))
            IssueDraw(graphics3D, item);
    }

    private static void DrawTransparentModels(
        IContext context,
        IGraphics3D graphics3D,
        IModelFactory modelFactory,
        ITextureFactory? textureFactory,
        Vector3 cameraPosition)
    {
        var transparent = EnumerateModelDrawItems(
                context, modelFactory, textureFactory, static mode => mode == MaterialAlphaMode.Blend)
            .ToList();
        if (transparent.Count == 0)
            return;

        TransparentDrawSort.SortBackToFront(transparent, cameraPosition, static item => item.WorldPosition);

        graphics3D.BeginTransparentPass();
        try
        {
            foreach (var item in transparent)
                IssueDraw(graphics3D, item);
        }
        finally
        {
            graphics3D.EndTransparentPass();
        }
    }

    private static void IssueDraw(IGraphics3D graphics3D, in ModelDrawItem item)
    {
        switch (item.Kind)
        {
            case ModelDrawKind.Cube:
                graphics3D.DrawCube(item.Transform, item.Tint, item.EntityId, item.AlbedoOverride, item.Metallic, item.Roughness);
                break;
            case ModelDrawKind.BuiltinSphere:
                graphics3D.DrawBuiltinSphere(
                    item.Transform, item.Tint, item.Metallic, item.Roughness, item.EntityId, item.AlbedoOverride);
                break;
            case ModelDrawKind.Mesh:
                graphics3D.DrawMesh(
                    item.Transform,
                    item.Mesh!,
                    item.Material!,
                    item.Tint,
                    item.Metallic,
                    item.Roughness,
                    item.EntityId,
                    item.BonePalette,
                    item.AlbedoOverride);
                break;
        }
    }

    private static IEnumerable<ModelDrawItem> EnumerateModelDrawItems(
        IContext context,
        IModelFactory modelFactory,
        ITextureFactory? textureFactory,
        Func<MaterialAlphaMode, bool> alphaFilter)
    {
        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            var transform = transformComponent.GetWorldTransform();
            var worldPosition = new Vector3(transform.M41, transform.M42, transform.M43);
            var tint = modelRenderer.Color;
            var albedo = TryLoadAlbedoOverride(textureFactory, modelRenderer.AlbedoTexturePath);

            if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
            {
                yield return new ModelDrawItem(
                    ModelDrawKind.Cube,
                    transform,
                    worldPosition,
                    null,
                    null,
                    tint,
                    modelRenderer.MetallicOverride ?? 0f,
                    modelRenderer.RoughnessOverride ?? 0.5f,
                    entity.Id,
                    AlbedoOverride: albedo);
                continue;
            }

            if (string.Equals(modelRenderer.ModelPath, BuiltinSphereModelPath, StringComparison.OrdinalIgnoreCase))
            {
                // Graphics3D.DrawBuiltinSphere uses default MeshMaterial (Opaque).
                if (alphaFilter(MaterialAlphaMode.Opaque))
                {
                    yield return new ModelDrawItem(
                        ModelDrawKind.BuiltinSphere,
                        transform,
                        worldPosition,
                        null,
                        null,
                        tint,
                        modelRenderer.MetallicOverride ?? 0f,
                        modelRenderer.RoughnessOverride ?? 0.5f,
                        entity.Id,
                        AlbedoOverride: albedo);
                }
                continue;
            }

            var model = MeshAsset.TryLoad(modelFactory, modelRenderer.ModelPath);
            if (model == null)
            {
                yield return new ModelDrawItem(
                    ModelDrawKind.Cube,
                    transform,
                    worldPosition,
                    null,
                    null,
                    tint,
                    modelRenderer.MetallicOverride ?? 0f,
                    modelRenderer.RoughnessOverride ?? 0.5f,
                    entity.Id,
                    AlbedoOverride: albedo);
                continue;
            }

            LogModelTintOnce(entity, modelRenderer, tint, model);

            var bonePalette = modelRenderer.BonePalette;

            foreach (var submesh in EnumerateDrawSubmeshes(model, modelRenderer))
            {
                if (!alphaFilter(submesh.Material.AlphaMode))
                    continue;

                var metallic = modelRenderer.MetallicOverride ?? submesh.Material.Metallic;
                var roughness = modelRenderer.RoughnessOverride ?? submesh.Material.Roughness;
                yield return new ModelDrawItem(
                    ModelDrawKind.Mesh,
                    transform,
                    worldPosition,
                    submesh.Mesh,
                    submesh.Material,
                    tint,
                    metallic,
                    roughness,
                    entity.Id,
                    bonePalette,
                    albedo);
            }
        }
    }

    private static Texture2D? TryLoadAlbedoOverride(ITextureFactory? textureFactory, string? path)
    {
        if (textureFactory == null || string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var resolved = PathBuilder.Resolve(path);
            if (!PathBuilder.IsUnderAssets(resolved))
            {
                Logger.Warning("Rejected albedo override outside assets root: {Path}", path);
                return null;
            }

            return textureFactory.Create(resolved, sRgb: true);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load albedo override '{Path}'", path);
            return null;
        }
    }

    private static void LogModelTintOnce(Entity entity, ModelRendererComponent modelRenderer, Vector4 tint, Model model)
    {
        if (!WarnedTintEntities.Add(entity.Id))
            return;

        if (tint.W <= 0f)
        {
            Logger.Warning(
                "ModelRenderer Color.a={Alpha} makes mesh invisible entity={EntityId} path={Path} color={Color}",
                tint.W, entity.Id, modelRenderer.ModelPath, tint);
        }
        else if (tint is { X: <= 0f, Y: <= 0f, Z: <= 0f })
        {
            Logger.Warning(
                "ModelRenderer Color.rgb is black entity={EntityId} path={Path} color={Color} — mesh draws but looks invisible",
                entity.Id, modelRenderer.ModelPath, tint);
        }
    }

    private static IEnumerable<ModelSubmesh> EnumerateDrawSubmeshes(Model model, ModelRendererComponent renderer)
    {
        var all = model.Submeshes;
        if (renderer.SubmeshCount < 0)
            return all;

        var start = System.Math.Clamp(renderer.SubmeshStart, 0, all.Count);
        var count = System.Math.Min(renderer.SubmeshCount, all.Count - start);
        if (count <= 0)
            return [];

        return all.Skip(start).Take(count);
    }

    private static Vector3 GetCameraPosition(in CameraBinding camera) =>
        camera.ViewCamera?.GetPosition()
        ?? new Vector3(camera.Transform.M41, camera.Transform.M42, camera.Transform.M43);

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

    internal static void Begin3DScene(IGraphics3D graphics3D, in CameraBinding camera)
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

        return (Vector3.One, 0.35f);
    }

    private static (Vector3 Direction, Vector3 Color, float Strength) ResolveDirectional(IContext context)
    {
        foreach (var (_, dlc) in context.View<DirectionalLightComponent>())
        {
            var direction = dlc.Direction.LengthSquared() < 1e-6f
                ? new Vector3(0, -1, 0)
                : Vector3.Normalize(dlc.Direction);
            return (direction, dlc.Color, dlc.Strength);
        }

        return (new Vector3(0, -1, 0), Vector3.Zero, 0f);
    }

    private static (Vector3 Position, Vector3 Color, float Strength, float Range) ResolvePoint(IContext context)
    {
        foreach (var (_, plc, transform) in context.View<PointLightComponent, TransformComponent>())
        {
            var world = transform.GetWorldTransform();
            return (new Vector3(world.M41, world.M42, world.M43), plc.Color, plc.Strength, plc.Range);
        }

        return (Vector3.Zero, Vector3.Zero, 0f, 25f);
    }

    private static (string? Path, float Intensity) ResolveSkyLight(IContext context)
    {
        foreach (var (_, slc) in context.View<SkyLightComponent>())
        {
            var path = string.IsNullOrWhiteSpace(slc.HdrPath) ? null : PathBuilder.Resolve(slc.HdrPath);
            return (path, slc.Intensity);
        }

        return (null, 1f);
    }
}
