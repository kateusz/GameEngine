using System.Numerics;
using ECS;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Lighting;
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
        Context context,
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory  modelFactory,
        in SceneView view)
    {
        RenderSpritesAndSubTextures(context, graphics2D, textureFactory, view);
        Render3D(context, graphics3D, textureFactory, modelFactory, view);
    }
    
    private static void RenderSpritesAndSubTextures(
        Context context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in SceneView view)
    {
        graphics2D.BeginScene(view);
        RenderSpritesInternal(context, graphics2D, textureFactory);
        RenderSubTexturesInternal(context, graphics2D, textureFactory);
        graphics2D.EndScene();
    }

    private static void RenderSpritesInternal(
        Context context,
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
        Context context,
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
        Context context,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory? modelFactory,
        in SceneView view)
    {
        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        graphics3D.SetAmbientLight(ambientColor, ambientStrength);
        var (lightDirection, lightColor) = ResolveDirectional(context);
        graphics3D.SetDirectionalLight(lightDirection, lightColor);
        graphics3D.BeginScene(view);

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
            var model = modelFactory.Create(resolvedPath);
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

        graphics3D.EndScene();
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

    internal static (Vector3 Color, float Strength) ResolveAmbient(Context context)
    {
        foreach (var (_, alc) in context.View<AmbientLightComponent>())
            return (new Vector3(alc.Color.X, alc.Color.Y, alc.Color.Z), alc.Strength);

        return (Vector3.One, 0.1f);
    }

    internal static (Vector3 Direction, Vector3 Color) ResolveDirectional(Context context)
    {
        foreach (var (_, dlc) in context.View<DirectionalLightComponent>())
            return (LightingMath.NormalizeDirection(dlc.Direction), new Vector3(dlc.Color.X, dlc.Color.Y, dlc.Color.Z));

        return (LightingMath.DefaultDirection, Vector3.Zero);
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
