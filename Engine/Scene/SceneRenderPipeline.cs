using System.Diagnostics;
using System.Numerics;
using ECS;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Meshes;
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
    private static readonly Dictionary<Mesh, int> MeshDrawCounts = new();
    private static long _nextDrawLogTicks;

    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];
    
    private static readonly PointLightData[] PointLightBuffer = new PointLightData[LightingMath.MaxPointLights];
    private static bool _shadowFitWarned;

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
        
        var pointCount = ResolvePointLights(context, PointLightBuffer);
        graphics3D.SetPointLights(PointLightBuffer.AsSpan(0, pointCount));

        var logFrame = Environment.TickCount64 >= _nextDrawLogTicks;
        if (logFrame)
            MeshDrawCounts.Clear();

        graphics3D.SetDirectionalShadow(Matrix4x4.Identity, false);
        PassStats? shadow = null;
        if (lightColor != Vector3.Zero &&
            LightingMath.TryFitDirectionalShadow(view.ViewProjection, lightDirection, out var lightViewProjection))
        {
            graphics3D.BeginShadowPass(lightViewProjection);
            shadow = DrawOpaque3D(context, graphics3D, textureFactory, modelFactory, meshDrawCounts: null);
            graphics3D.EndShadowPass();
            graphics3D.SetDirectionalShadow(lightViewProjection, true);
        }
        else if (lightColor != Vector3.Zero && !_shadowFitWarned)
        {
            _shadowFitWarned = true;
            Logger.Warning("Directional shadow fit failed; drawing the frame without directional shadows");
        }

        graphics3D.BeginScene(view);
        var color = DrawOpaque3D(
            context, graphics3D, textureFactory, modelFactory,
            meshDrawCounts: logFrame ? MeshDrawCounts : null);
        graphics3D.EndScene();

        if (!logFrame)
            return;

        _nextDrawLogTicks = Environment.TickCount64 + 1000;
        if (color.MeshDraws == 0 && color.CubeDraws == 0 && (shadow is null || shadow.Value.MeshDraws == 0))
            return;

        LogDrawStats(shadow, color);
    }

    private struct PassStats
    {
        public int Renderers;
        public int MeshDraws;
        public int CubeDraws;
        public int MissingMeshIndex;
        public long Vertices;
        public long Indices;
        public double CpuMs;
    }

    private static PassStats DrawOpaque3D(
        Context context,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory? modelFactory,
        Dictionary<Mesh, int>? meshDrawCounts)
    {
        var stats = new PassStats();
        var start = Stopwatch.GetTimestamp();
        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            stats.Renderers++;
            var transform = transformComponent.GetWorldTransform();

            if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
            {
                var factors = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
                if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
                    DrawCubeWithTexture(graphics3D, textureFactory, modelRenderer, transform, entity, factors);
                else
                    graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id,
                        metallic: factors.Metallic, roughness: factors.Roughness, ao: factors.Ao);
                stats.CubeDraws++;
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
                var fallback = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
                graphics3D.DrawCube(transform, tint, entity.Id,
                    metallic: fallback.Metallic, roughness: fallback.Roughness, ao: fallback.Ao);
                stats.CubeDraws++;
                continue;
            }

            if (modelRenderer.MeshIndex is int meshIndex)
            {
                if (meshIndex >= 0 && meshIndex < model.Submeshes.Count)
                    DrawSubmesh(graphics3D, modelRenderer, transform, tint, entity.Id, model.Submeshes[meshIndex],
                        ref stats, meshDrawCounts);
                else
                    stats.MissingMeshIndex++;
                continue;
            }

            if (modelRenderer.SuppressDraw)
                continue;

            foreach (var submesh in model.Submeshes)
                DrawSubmesh(graphics3D, modelRenderer, transform, tint, entity.Id, submesh, ref stats, meshDrawCounts);
        }

        stats.CpuMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        return stats;
    }

    private static void DrawSubmesh(
        IGraphics3D graphics3D,
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        ref PassStats stats,
        Dictionary<Mesh, int>? meshDrawCounts)
    {
        stats.MeshDraws++;
        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.GetIndexCount();
        if (meshDrawCounts != null)
        {
            meshDrawCounts.TryGetValue(submesh, out var count);
            meshDrawCounts[submesh] = count + 1;
        }

        var pbr = ResolvePbr(cube: false, modelRenderer, submesh.MetallicFactor, submesh.RoughnessFactor);
        graphics3D.DrawMesh(transform, submesh, tint, entityId, pbr.Metallic, pbr.Roughness, pbr.Ao);
    }

    private static void LogDrawStats(PassStats? shadow, PassStats color)
    {
        if (shadow is { } shadowPass)
            Logger.Information(
                "3D shadow pass renderers={Renderers} meshDraws={MeshDraws} cubeDraws={CubeDraws} missingMeshIndex={MissingMeshIndex} vertices={Vertices} indices={Indices} triangles={Triangles} cpuMs={CpuMs:0.0}",
                shadowPass.Renderers, shadowPass.MeshDraws, shadowPass.CubeDraws, shadowPass.MissingMeshIndex,
                shadowPass.Vertices, shadowPass.Indices, shadowPass.Indices / 3, shadowPass.CpuMs);

        Logger.Information(
            "3D color pass renderers={Renderers} meshDraws={MeshDraws} cubeDraws={CubeDraws} uniqueMeshes={UniqueMeshes} missingMeshIndex={MissingMeshIndex} vertices={Vertices} indices={Indices} triangles={Triangles} cpuMs={CpuMs:0.0}",
            color.Renderers, color.MeshDraws, color.CubeDraws, MeshDrawCounts.Count, color.MissingMeshIndex,
            color.Vertices, color.Indices, color.Indices / 3, color.CpuMs);

        foreach (var pair in MeshDrawCounts.OrderByDescending(p => p.Value).Take(8))
        {
            var mesh = pair.Key;
            var indexCount = mesh.GetIndexCount();
            Logger.Information(
                "3D repeated mesh draws={Draws} vertices={Vertices} indices={Indices} triangles={Triangles} submittedVertices={SubmittedVertices} name={Name}",
                pair.Value, mesh.VertexCount, indexCount, indexCount / 3,
                (long)pair.Value * mesh.VertexCount, mesh.Name);
        }
    }

    private static void DrawCubeWithTexture(IGraphics3D graphics3D, ITextureFactory textureFactory,
        ModelRendererComponent modelRenderer, Matrix4x4 transform, Entity entity, PbrFactors factors)
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
                modelRenderer.TilingFactor,
                factors.Metallic,
                factors.Roughness,
                factors.Ao);
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

    internal readonly record struct PbrFactors(float Metallic, float Roughness, float Ao);

    internal static PbrFactors ResolvePbr(
        bool cube,
        ModelRendererComponent renderer,
        float meshMetallic,
        float meshRoughness)
    {
        var entityMetallic = Finite01(renderer.Metallic);
        var entityRoughness = Finite01(renderer.Roughness);
        var entityAo = Finite01(renderer.Ao);
        if (cube || renderer.OverrideMaterial)
            return new PbrFactors(entityMetallic, entityRoughness, entityAo);

        return new PbrFactors(Finite01(meshMetallic), Finite01(meshRoughness), 1f);
    }

    private static float Finite01(float value) =>
        float.IsFinite(value) ? System.Math.Clamp(value, 0f, 1f) : 0f;
    
    internal static int ResolvePointLights(Context context, Span<PointLightData> destination)
    {
        var limit = System.Math.Min(destination.Length, LightingMath.MaxPointLights);
        var count = 0;
        foreach (var (_, light, transform) in context.View<PointLightComponent, TransformComponent>())
        {
            if (light.Range <= 0f)
                continue;
            if (count == limit)
                break;

            destination[count++] = new PointLightData(
                transform.GetWorldTransform().Translation,
                new Vector3(light.Color.X, light.Color.Y, light.Color.Z),
                MathF.Max(0f, light.Intensity),
                light.Range);
        }

        return count;
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
