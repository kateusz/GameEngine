using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal static class SceneRenderPipeline
{
    private static readonly ILogger Logger = Log.ForContext(typeof(SceneRenderPipeline));

    private static readonly HashSet<string> WarnedFailedModels = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];

    public static void RenderScene(
        IContext context,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory  modelFactory,
        in SceneView view)
    {
        RenderSpritesAndSubTextures(context, graphics2D, textureFactory, view);
        Render3D(context, graphics3D, textureFactory, modelFactory, view);
    }
    
    internal static void Begin2DScene(IGraphics2D graphics2D, in SceneView view) =>
        graphics2D.BeginScene(view);
    
    private static void RenderSpritesAndSubTextures(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in SceneView view)
    {
        Begin2DScene(graphics2D, view);
        RenderTileMapsInternal(context, graphics2D, textureFactory);
        RenderSpritesInternal(context, graphics2D, textureFactory);
        RenderSubTexturesInternal(context, graphics2D, textureFactory);
        graphics2D.EndScene();
    }

    private static void RenderTileMapsInternal(
        IContext context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory)
    {
        var uv = new Vector2[RenderingConstants.QuadVertexCount];
        foreach (var (entity, tilemap, transformComponent) in
                 context.View<TileMapComponent, TransformComponent>())
        {
            tilemap.Repair();
            var mapWorld = transformComponent.GetWorldTransform();
            foreach (var layer in tilemap.Layers)
            {
                if (!layer.Visible || textureFactory == null || string.IsNullOrWhiteSpace(layer.TexturePath))
                    continue;

                Texture2D texture;
                try
                {
                    texture = textureFactory.Create(PathBuilder.Resolve(layer.TexturePath!));
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex, "Failed to load tilemap texture '{TexturePath}'", layer.TexturePath);
                    continue;
                }

                var tileSize = layer.TileSize > 0 ? layer.TileSize : tilemap.TileSize;
                for (var y = 0; y < tilemap.Height; y++)
                {
                    for (var x = 0; x < tilemap.Width; x++)
                    {
                        var i = y * tilemap.Width + x;
                        if ((uint)i >= (uint)layer.Tiles.Length)
                            continue;

                        var flags = (uint)i < (uint)layer.Flags.Length ? layer.Flags[i] : (byte)0;
                        if (!TilesetUv.TryGetUvRect(
                                layer.Tiles[i], texture.Width, texture.Height, tileSize, layer.Margin, layer.Spacing,
                                (flags & TileMapComponent.FlipH) != 0,
                                (flags & TileMapComponent.FlipV) != 0,
                                uv))
                            continue;

                        var cell = Matrix4x4.CreateTranslation(x + 0.5f, y + 0.5f, 0f) * mapWorld;
                        graphics2D.DrawQuad(cell, texture, uv, 1f, Vector4.One, entity.Id);
                    }
                }
            }
        }
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
                    var resolved = GetResolvedSpriteTexturePath(spriteRendererComponent);
                    var texture = textureFactory.Create(resolved);
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
            var texCoords = GetSubTextureTexCoords(subtextureComponent, texture);

            graphics2D.DrawQuad(transform, texture, texCoords, 1.0f, Vector4.One, entity.Id);
        }
    }
    
    private static void Render3D(
        IContext context,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory? modelFactory,
        in SceneView view)
    {
        graphics3D.BeginScene(view);
        var lighting = SceneLightingResolver.Resolve(context);
        graphics3D.SetAmbientLight(lighting.AmbientColor, lighting.AmbientStrength);
        graphics3D.SetDirectionalLight(lighting.DirectionalDirection, lighting.DirectionalColor);

        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            var transform = transformComponent.GetWorldTransform();

            if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
            {
                if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
                    DrawCubeWithTexture(graphics3D, textureFactory, modelRenderer, transform, entity);
                else
                    graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id);
                continue;
            }

            if (modelFactory == null)
                continue;

            var tint = modelRenderer.Color;
            var resolvedPath = PathBuilder.Resolve(modelRenderer.ModelPath);
            var model = modelFactory.Create(resolvedPath, modelRenderer.MergeByMaterial);
            if (model == null)
            {
                if (WarnedFailedModels.Add(resolvedPath))
                    Logger.Warning(
                        "Failed to load model assetPath={ModelPath} resolved={ResolvedPath} — drawing unit cube instead",
                        modelRenderer.ModelPath, resolvedPath);
                graphics3D.DrawCube(transform, tint, entity.Id);
                continue;
            }

            if (modelRenderer.MeshIndex is int meshIndex)
            {
                if (meshIndex >= 0 && meshIndex < model.Submeshes.Count)
                    graphics3D.DrawMesh(transform, model.Submeshes[meshIndex], tint, entity.Id);
                continue;
            }

            if (modelRenderer.SuppressDraw)
                continue;

            foreach (var submesh in model.Submeshes)
                graphics3D.DrawMesh(transform, submesh, tint, entity.Id);
        }

        RenderSkybox(context, graphics3D, textureFactory);

        graphics3D.EndScene();
    }

    private static void RenderSkybox(IContext context, IGraphics3D graphics3D, ITextureFactory textureFactory)
    {
        SkyboxComponent? skybox = null;
        Entity? skyboxEntity = null;
        foreach (var (entity, component) in context.View<SkyboxComponent>())
        {
            skybox = component;
            skyboxEntity = entity;
            break;
        }

        if (skybox == null || string.IsNullOrWhiteSpace(skybox.HdrPath))
            return;

        try
        {
            var hdr = textureFactory.Create(PathBuilder.Resolve(skybox.HdrPath), sRgb: false);
            var yaw = skyboxEntity!.TryGetComponent<TransformComponent>(out var transform)
                ? transform.Rotation.Y
                : 0f;
            graphics3D.DrawSkybox(hdr, skybox.Intensity, yaw);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load skybox HDR '{HdrPath}'", skybox.HdrPath);
        }
    }

    private static void DrawCubeWithTexture(IGraphics3D graphics3D, ITextureFactory textureFactory,
        ModelRendererComponent modelRenderer, Matrix4x4 transform, Entity entity)
    {
        try
        {
            var texture = textureFactory.Create(
                PathBuilder.Resolve(modelRenderer.TexturePath!), sRgb: true);
            graphics3D.DrawCube(
                transform,
                modelRenderer.Color,
                entity.Id,
                texture,
                modelRenderer.TilingFactor);
        }
        catch (Exception ex)
        {
            Logger.Warning(
                ex,
                "Failed to load cube texture '{TexturePath}' — drawing solid color instead",
                modelRenderer.TexturePath);
        }
    }

    internal static Vector2[] GetSubTextureTexCoords(SubTextureRendererComponent component, Texture2D texture)
    {
        if (component.TexCoordsCacheKey == SubTextureRendererComponent.ManualTexCoordsKey && component.TexCoords != null)
            return component.TexCoords;

        var key = HashSubTextureTexCoords(component, texture);
        if (component.TexCoords != null && component.TexCoordsCacheKey == key)
            return component.TexCoords;

        if (component.TexCoords is not { Length: RenderingConstants.QuadVertexCount })
            component.TexCoords = new Vector2[RenderingConstants.QuadVertexCount];

        SubTexture2D.FillTexCoordsFromCoords(
            texture, component.Coords, component.CellSize, component.SpriteSize, component.TexCoords);
        component.TexCoordsCacheKey = key;
        return component.TexCoords;
    }

    private static int HashSubTextureTexCoords(SubTextureRendererComponent component, Texture2D texture) =>
        HashCode.Combine(
            component.Coords.X, component.Coords.Y,
            component.CellSize.X, component.CellSize.Y,
            component.SpriteSize.X, component.SpriteSize.Y,
            texture.Width, texture.Height);

    internal static string? GetResolvedSpriteTexturePath(SpriteRendererComponent component)
    {
        var path = component.TexturePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            component.ResolvedTexturePath = null;
            return null;
        }

        component.ResolvedTexturePath ??= PathBuilder.Resolve(path);
        return component.ResolvedTexturePath;
    }

}
