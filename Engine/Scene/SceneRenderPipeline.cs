using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
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

    private static readonly Vector2[] DefaultTextureCoords =
    [
        new(0.0f, 0.0f),
        new(1.0f, 0.0f),
        new(1.0f, 1.0f),
        new(0.0f, 1.0f)
    ];
    
    private static readonly PointLightData[] PointLightBuffer = new PointLightData[LightingMath.MaxPointLights];
    private static readonly List<(int Id, PointShadowCache.CasterPose Pose)> PointShadowCasterBuffer = new();
    private static readonly List<(int Id, PointShadowCache.LampPose Pose)> PointShadowLampBuffer = new();
    private static readonly List<int> ActiveVisibilityZoneEntityIds = new();
    private static readonly HashSet<int> WarnedMissingVisibilityZoneEntityIds = new();
    private static bool _visibilityZonesEnabled;
    private static bool _shadowFitWarned;
    private static bool _pointShadowWarned;
    private static long _nextPerfLogTicks;
    private const int PerfLogIntervalMs = 1000;

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
        var perf = new Render3DPerfFrame();
        PrepareActiveVisibilityZones(context, view.ViewPosition);

        var (ambientColor, ambientStrength) = ResolveAmbient(context);
        graphics3D.SetAmbientLight(ambientColor, ambientStrength);
        
        var (lightDirection, lightColor) = ResolveDirectional(context);
        graphics3D.SetDirectionalLight(lightDirection, lightColor);
        
        var pointCount = ResolvePointLights(context, PointLightBuffer);
        graphics3D.SetPointLights(PointLightBuffer.AsSpan(0, pointCount));
        perf.PointLights = pointCount;
        for (var i = 0; i < pointCount; i++)
        {
            if (PointLightBuffer[i].CastsShadow)
                perf.PointShadowLights++;
        }

        var shadowCasterMax = view.DirectionalShadowCasterMaxDistance;
        var shadowCasterMaxSq = shadowCasterMax > 0f ? shadowCasterMax * shadowCasterMax : 0f;
        var shadowCasterView = view.ViewPosition;

        graphics3D.SetDirectionalShadow(Matrix4x4.Identity, false);
        if (lightColor != Vector3.Zero &&
            LightingMath.TryFitDirectionalShadow(view.ViewProjection, lightDirection, out var lightViewProjection))
        {
            perf.DirectionalShadow = true;
            graphics3D.BeginShadowPass(lightViewProjection);
            perf.DirectionalShadowPass = DrawOpaque3D(
                context, graphics3D, textureFactory, modelFactory, lightViewProjection,
                shadowCasterMaxSq, shadowCasterView);
            graphics3D.EndShadowPass();
            graphics3D.SetDirectionalShadow(lightViewProjection, true);
        }
        else if (lightColor != Vector3.Zero && !_shadowFitWarned)
        {
            _shadowFitWarned = true;
            Logger.Warning("Directional shadow fit failed; drawing the frame without directional shadows");
        }

        if (!view.PointShadows)
        {
            PointShadowCache.MarkStale();
        }
        else
        {
            CollectPointShadowCasters(context, modelFactory, PointShadowCasterBuffer);
            perf.PointShadowCasters = PointShadowCasterBuffer.Count;
            PointShadowLampBuffer.Clear();
            for (var li = 0; li < pointCount; li++)
            {
                var resolved = PointLightBuffer[li];
                if (!resolved.CastsShadow)
                    continue;
                PointShadowLampBuffer.Add((resolved.EntityId, new PointShadowCache.LampPose(resolved.Position, resolved.Range)));
            }

            Span<Matrix4x4> pointFaces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
            for (var i = 0; i < pointCount; i++)
            {
                var light = PointLightBuffer[i];
                if (!light.CastsShadow)
                    continue;

                var dirty = PointShadowCache.NeedsRedraw(
                    light.EntityId, light.Position, light.Range, PointShadowCasterBuffer);
                var near = Vector3.Distance(view.ViewPosition, light.Position) <= LightingMath.PointShadowDistance;
                if (!dirty)
                {
                    if (near)
                        PointShadowCache.RememberClean(light.EntityId);
                    if (near && !graphics3D.UseCachedPointShadow(i, light.EntityId))
                        dirty = true;
                    else
                    {
                        perf.PointShadowLightsCacheHit++;
                        continue;
                    }
                }

                PointShadowCache.RememberDirty(light.EntityId);
                if (!near)
                {
                    perf.PointShadowLightsTooFar++;
                    continue;
                }

                if (!LightingMath.TryBuildPointShadowFaces(light.Position, light.Range, pointFaces))
                    continue;

                perf.PointShadowLightsRedrawn++;
                var drew = true;
                for (var face = 0; face < LightingMath.PointShadowFaceCount; face++)
                {
                    if (!graphics3D.BeginPointShadowFace(
                            i, light.EntityId, face, pointFaces[face], light.Position, light.Range))
                    {
                        if (!_pointShadowWarned)
                        {
                            _pointShadowWarned = true;
                            Logger.Warning("Point shadow cubemap failed; drawing that light without a shadow");
                        }

                        drew = false;
                        break;
                    }

                    try
                    {
                        perf.PointShadowFaces++;
                        perf.PointShadowOpaque3D++;
                        perf.PointShadowPass = Accumulate(perf.PointShadowPass,
                            DrawOpaque3D(context, graphics3D, textureFactory, modelFactory, pointFaces[face],
                                shadowCasterMaxSq, shadowCasterView));
                    }
                    finally
                    {
                        graphics3D.EndPointShadowFace();
                    }
                }

                if (drew)
                    PointShadowCache.RememberClean(light.EntityId);
            }

            PointShadowCache.Replace(PointShadowCasterBuffer, PointShadowLampBuffer);
        }

        graphics3D.BeginScene(view);
        perf.ColorPass = DrawOpaque3D(context, graphics3D, textureFactory, modelFactory, view.ViewProjection);
        graphics3D.EndScene();

        perf.GpuDrawCalls = graphics3D.GetStats().DrawCalls;
        LogRender3DPerfIfDue(in perf);
    }

    private struct PassStats
    {
        public int Renderers;
        public int MeshDraws;
        public int Instances;
        public int CubeDraws;
        public int Culled;
        public int MissingMeshIndex;
        public int ShadowCasterCulled;
        public int ZoneCulled;
        public int SingleMaterialDraws;
        public int MultiMaterialDraws;
        public int MaxBatchInstances;
        public long Vertices;
        public long Indices;
        public double CpuMs;
    }

    private struct Render3DPerfFrame
    {
        public bool DirectionalShadow;
        public PassStats DirectionalShadowPass;
        public PassStats ColorPass;
        public PassStats PointShadowPass;
        public int PointLights;
        public int PointShadowLights;
        public int PointShadowCasters;
        public int PointShadowLightsCacheHit;
        public int PointShadowLightsTooFar;
        public int PointShadowLightsRedrawn;
        public int PointShadowFaces;
        public int PointShadowOpaque3D;
        public uint GpuDrawCalls;
    }

    private static PassStats Accumulate(PassStats total, PassStats pass)
    {
        total.Renderers += pass.Renderers;
        total.MeshDraws += pass.MeshDraws;
        total.Instances += pass.Instances;
        total.CubeDraws += pass.CubeDraws;
        total.Culled += pass.Culled;
        total.MissingMeshIndex += pass.MissingMeshIndex;
        total.ShadowCasterCulled += pass.ShadowCasterCulled;
        total.ZoneCulled += pass.ZoneCulled;
        total.SingleMaterialDraws += pass.SingleMaterialDraws;
        total.MultiMaterialDraws += pass.MultiMaterialDraws;
        total.MaxBatchInstances = System.Math.Max(total.MaxBatchInstances, pass.MaxBatchInstances);
        total.Vertices += pass.Vertices;
        total.Indices += pass.Indices;
        total.CpuMs += pass.CpuMs;
        return total;
    }

    private static void LogRender3DPerfIfDue(in Render3DPerfFrame perf)
    {
        var now = Environment.TickCount64;
        if (now < _nextPerfLogTicks)
            return;

        _nextPerfLogTicks = now + PerfLogIntervalMs;

        var dirRes = LightingMath.ShadowMapResolution;
        var pointFaceRes = LightingMath.PointShadowFaceResolution;
        long shadowFillPixels = 0;
        if (perf.DirectionalShadow)
            shadowFillPixels += (long)dirRes * dirRes;
        shadowFillPixels += (long)perf.PointShadowFaces * pointFaceRes * pointFaceRes;

        var color = perf.ColorPass;
        var dir = perf.DirectionalShadowPass;
        var point = perf.PointShadowPass;
        var materialDrawsColor = color.MeshDraws + color.CubeDraws;
        var opaque3DPasses = (perf.DirectionalShadow ? 1 : 0) + perf.PointShadowOpaque3D + 1;

        Logger.Information(
            "3D perf: gpuDrawCalls={GpuDrawCalls} opaque3DPasses={Opaque3DPasses} " +
            "dirShadow={DirShadow} dirRes={DirRes}x{DirRes} dirMeshDraws={DirMeshDraws} dirCubes={DirCubes} dirCulled={DirCulled} dirShadowCasterCulled={DirShadowCasterCulled} " +
            "colorMeshDraws={ColorMeshDraws} colorCubes={ColorCubes} colorCulled={ColorCulled} colorZoneCulled={ColorZoneCulled} " +
            "materialBatches single={SingleBatch} multi={MultiBatch} maxInstBatch={MaxBatch} meshInstances={Instances} " +
            "pointShadow lights={PointShadowLights} casters={Casters} cacheHit={CacheHit} tooFar={TooFar} redrawn={Redrawn} " +
            "fullSceneFaces={Faces} faceRes={PointRes}x{PointRes} pointMeshDraws={PointMeshDraws} estShadowFillPx={ShadowFillPx} " +
            "cpuMs dir={DirCpu:F2} color={ColorCpu:F2} point={PointCpu:F2}",
            perf.GpuDrawCalls,
            opaque3DPasses,
            perf.DirectionalShadow,
            dirRes,
            dirRes,
            dir.MeshDraws + dir.CubeDraws,
            dir.CubeDraws,
            dir.Culled,
            dir.ShadowCasterCulled,
            materialDrawsColor,
            color.CubeDraws,
            color.Culled,
            color.ZoneCulled,
            color.SingleMaterialDraws,
            color.MultiMaterialDraws,
            color.MaxBatchInstances,
            color.Instances,
            perf.PointShadowLights,
            perf.PointShadowCasters,
            perf.PointShadowLightsCacheHit,
            perf.PointShadowLightsTooFar,
            perf.PointShadowLightsRedrawn,
            perf.PointShadowFaces,
            pointFaceRes,
            pointFaceRes,
            point.MeshDraws + point.CubeDraws,
            shadowFillPixels,
            dir.CpuMs,
            color.CpuMs,
            point.CpuMs);
    }

    private enum OpaqueSurfaceKind
    {
        UnitCube,
        SingleSubmesh,
        AllSubmeshes
    }

    private static bool TryResolveOpaqueSurface(
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        IModelFactory? modelFactory,
        out PointShadowCache.CasterPose casterPose,
        out OpaqueSurfaceKind kind,
        out Model? model,
        out int submeshIndex)
    {
        casterPose = default;
        kind = default;
        model = null;
        submeshIndex = -1;

        if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
        {
            casterPose = new PointShadowCache.CasterPose(transform, Aabb.UnitCube, true);
            kind = OpaqueSurfaceKind.UnitCube;
            return true;
        }

        if (modelFactory == null)
            return false;

        var resolvedPath = PathBuilder.Resolve(modelRenderer.ModelPath);
        model = modelFactory.Create(resolvedPath);
        if (model == null)
        {
            casterPose = new PointShadowCache.CasterPose(transform, Aabb.UnitCube, true);
            kind = OpaqueSurfaceKind.UnitCube;
            return true;
        }

        if (modelRenderer.MeshIndex is int meshIndex)
        {
            if (meshIndex < 0 || meshIndex >= model.Submeshes.Count)
                return false;

            submeshIndex = meshIndex;
            var submesh = model.Submeshes[meshIndex];
            casterPose = submesh.Bounds is { } singleBounds
                ? new PointShadowCache.CasterPose(transform, singleBounds, true)
                : new PointShadowCache.CasterPose(transform, default, false);
            kind = OpaqueSurfaceKind.SingleSubmesh;
            return true;
        }

        if (modelRenderer.SuppressDraw)
            return false;

        var hasBounds = true;
        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var submesh in model.Submeshes)
        {
            if (submesh.Bounds is not { } bounds)
            {
                hasBounds = false;
                break;
            }

            min = Vector3.Min(min, bounds.Min);
            max = Vector3.Max(max, bounds.Max);
        }

        casterPose = hasBounds
            ? new PointShadowCache.CasterPose(transform, new Aabb(min, max), true)
            : new PointShadowCache.CasterPose(transform, default, false);
        kind = OpaqueSurfaceKind.AllSubmeshes;
        return true;
    }

    private static PassStats DrawOpaque3D(
        Context context,
        IGraphics3D graphics3D,
        ITextureFactory textureFactory,
        IModelFactory? modelFactory,
        Matrix4x4 cullMatrix,
        float shadowCasterMaxDistanceSq = 0f,
        Vector3 shadowCasterViewPosition = default)
    {
        var stats = new PassStats();
        var start = Stopwatch.GetTimestamp();
        var hasFrustum = Frustum.TryFromClip(cullMatrix, out var frustum);
        try
        {
            foreach (var (entity, modelRenderer, transformComponent) in
                     context.View<ModelRendererComponent, TransformComponent>())
            {
                stats.Renderers++;
                if (ShouldSkipForVisibilityZone(modelRenderer.VisibilityZoneEntityId, context, ref stats))
                    continue;

                var transform = ModelMeshPivot.ToDrawTransform(
                    transformComponent.GetWorldTransform(),
                    modelRenderer.Pivot);
                if (!TryResolveOpaqueSurface(
                        modelRenderer, transform, modelFactory,
                        out _, out var kind, out var model, out var submeshIndex))
                {
                    if (modelRenderer.MeshIndex is int meshIndex
                        && !string.IsNullOrWhiteSpace(modelRenderer.ModelPath)
                        && modelFactory != null)
                    {
                        var loaded = modelFactory.Create(PathBuilder.Resolve(modelRenderer.ModelPath));
                        if (loaded != null && (meshIndex < 0 || meshIndex >= loaded.Submeshes.Count))
                            stats.MissingMeshIndex++;
                    }

                    continue;
                }

                var tint = modelRenderer.Color;
                if (kind == OpaqueSurfaceKind.UnitCube)
                {
                    if (!string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
                    {
                        var resolvedPath = PathBuilder.Resolve(modelRenderer.ModelPath);
                        if (WarnedFailedModels.Add(resolvedPath))
                            Logger.Warning(
                                "Failed to load model assetPath={ModelPath} resolved={ResolvedPath} — drawing unit cube instead",
                                modelRenderer.ModelPath, resolvedPath);
                    }

                    if (ShouldSkipOpaqueDraw(hasFrustum, frustum, transform, Aabb.UnitCube, shadowCasterMaxDistanceSq,
                            shadowCasterViewPosition, ref stats))
                        continue;

                    var factors = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
                    if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
                        DrawCubeWithTexture(graphics3D, textureFactory, modelRenderer, transform, entity, factors);
                    else
                        graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id,
                            metallic: factors.Metallic, roughness: factors.Roughness, ao: factors.Ao);
                    stats.CubeDraws++;
                    continue;
                }

                if (kind == OpaqueSurfaceKind.SingleSubmesh)
                {
                    var submesh = model!.Submeshes[submeshIndex];
                    if (submesh.Bounds is { } bounds
                        && ShouldSkipOpaqueDraw(hasFrustum, frustum, transform, bounds, shadowCasterMaxDistanceSq,
                            shadowCasterViewPosition, ref stats))
                        continue;

                    QueueSubmesh(modelRenderer, transform, tint, entity.Id, submesh, ref stats);
                    continue;
                }

                foreach (var submesh in model!.Submeshes)
                {
                    if (submesh.Bounds is { } bounds
                        && ShouldSkipOpaqueDraw(hasFrustum, frustum, transform, bounds, shadowCasterMaxDistanceSq,
                            shadowCasterViewPosition, ref stats))
                        continue;

                    QueueSubmesh(modelRenderer, transform, tint, entity.Id, submesh, ref stats);
                }
            }
        }
        finally
        {
            FlushMeshBatches(graphics3D, ref stats);
        }

        stats.CpuMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        return stats;
    }

    private static bool ShouldSkipOpaqueDraw(
        bool hasFrustum,
        in Frustum frustum,
        Matrix4x4 world,
        Aabb bounds,
        float shadowCasterMaxDistanceSq,
        Vector3 shadowCasterViewPosition,
        ref PassStats stats)
    {
        if (hasFrustum && frustum.IsOutside(world, bounds))
        {
            stats.Culled++;
            return true;
        }

        if (shadowCasterMaxDistanceSq > 0f
            && Aabb.ClosestPointDistanceSquared(shadowCasterViewPosition, world, bounds) > shadowCasterMaxDistanceSq)
        {
            stats.ShadowCasterCulled++;
            return true;
        }

        return false;
    }

    private static void PrepareActiveVisibilityZones(Context context, Vector3 cameraPosition)
    {
        _visibilityZonesEnabled = false;
        ActiveVisibilityZoneEntityIds.Clear();
        foreach (var (entity, zone, transform) in context.View<VisibilityZoneComponent, TransformComponent>())
        {
            _visibilityZonesEnabled = true;
            var bounds = new Aabb(zone.Min, zone.Max);
            if (Aabb.ContainsPoint(cameraPosition, transform.GetWorldTransform(), bounds))
                ActiveVisibilityZoneEntityIds.Add(entity.Id);
        }
    }

    private static bool ShouldSkipForVisibilityZone(int visibilityZoneEntityId, Context context, ref PassStats stats)
    {
        if (!_visibilityZonesEnabled || visibilityZoneEntityId < 0)
            return false;

        if (ActiveVisibilityZoneEntityIds.Contains(visibilityZoneEntityId))
            return false;

        if (!context.Contains(visibilityZoneEntityId)
            && WarnedMissingVisibilityZoneEntityIds.Add(visibilityZoneEntityId))
        {
            Logger.Warning(
                "Model references missing visibility zone entity {ZoneEntityId}",
                visibilityZoneEntityId);
        }

        stats.ZoneCulled++;
        return true;
    }

    private readonly record struct MeshBatchKey(Mesh Mesh, Vector4 Tint, float Metallic, float Roughness, float Ao);

    private static readonly Dictionary<MeshBatchKey, List<MeshDrawInstance>> MeshBatches = new();
    private static readonly Stack<List<MeshDrawInstance>> BatchLists = new();

    private static void QueueSubmesh(
        ModelRendererComponent modelRenderer,
        Matrix4x4 transform,
        Vector4 tint,
        int entityId,
        Mesh submesh,
        ref PassStats stats)
    {
        stats.Instances++;
        stats.Vertices += submesh.VertexCount;
        stats.Indices += submesh.GetIndexCount();

        var pbr = ResolvePbr(cube: false, modelRenderer, submesh.MetallicFactor, submesh.RoughnessFactor);
        var key = new MeshBatchKey(submesh, tint, pbr.Metallic, pbr.Roughness, pbr.Ao);
        if (!MeshBatches.TryGetValue(key, out var batch))
        {
            batch = BatchLists.Count > 0 ? BatchLists.Pop() : [];
            MeshBatches[key] = batch;
        }

        batch.Add(new MeshDrawInstance
        {
            Transform = transform,
            EntityId = entityId,
            Tint = tint,
            Metallic = pbr.Metallic,
            Roughness = pbr.Roughness,
            Ao = pbr.Ao
        });
    }

    private static void FlushMeshBatches(IGraphics3D graphics3D, ref PassStats stats)
    {
        try
        {
            foreach (var (key, batch) in MeshBatches)
            {
                var count = batch.Count;
                if (count >= 2)
                {
                    stats.MultiMaterialDraws++;
                    stats.MaxBatchInstances = System.Math.Max(stats.MaxBatchInstances, count);
                }
                else
                    stats.SingleMaterialDraws++;

                graphics3D.DrawMeshInstances(key.Mesh, CollectionsMarshal.AsSpan(batch));
                stats.MeshDraws++;
            }
        }
        finally
        {
            foreach (var batch in MeshBatches.Values)
            {
                batch.Clear();
                BatchLists.Push(batch);
            }

            MeshBatches.Clear();
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
        foreach (var (entity, light, transform) in context.View<PointLightComponent, TransformComponent>())
        {
            if (light.Range <= 0f)
                continue;
            if (count == limit)
                break;

            destination[count++] = new PointLightData(
                transform.GetWorldTransform().Translation,
                new Vector3(light.Color.X, light.Color.Y, light.Color.Z),
                MathF.Max(0f, light.Intensity),
                light.Range,
                light.CastsShadow,
                entity.Id);
        }

        return count;
    }

    private static void CollectPointShadowCasters(
        Context context,
        IModelFactory? modelFactory,
        List<(int Id, PointShadowCache.CasterPose Pose)> destination)
    {
        destination.Clear();
        foreach (var (entity, modelRenderer, transformComponent) in
                 context.View<ModelRendererComponent, TransformComponent>())
        {
            var transform = ModelMeshPivot.ToDrawTransform(
                transformComponent.GetWorldTransform(),
                modelRenderer.Pivot);
            if (TryResolveOpaqueSurface(
                    modelRenderer, transform, modelFactory,
                    out var pose, out _, out _, out _))
                destination.Add((entity.Id, pose));
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
