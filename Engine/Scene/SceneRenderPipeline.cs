using System.Diagnostics;
using System.Numerics;
using ECS;
using Engine.Project;
using Engine.Renderer;
using Engine.Renderer.Buffers;
using Engine.Renderer.Buffers.VertexArray;
using Engine.Renderer.Meshes;
using Engine.Renderer.Models;
using Engine.Renderer.Pipeline;
using Engine.Renderer.Textures;
using SceneComponents;
using SceneComponents.Lighting;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

public sealed class SceneRenderPipeline : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<SceneRenderPipeline>();
    private readonly IGraphics2D graphics2D;
    private readonly IGraphics3D graphics3D;
    private readonly ITextureFactory textureFactory;
    private readonly SceneRenderDataBuilder _builder;
    private readonly SceneRenderState _state = new();
    private readonly MeshPassRenderer _meshPass;
    private bool _disposed;
    private readonly HashSet<string> WarnedFailedSprites = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<OrderedSprite> SpriteDrawList = new(256);
    private static readonly Comparison<OrderedSprite> SpriteDrawOrder = CompareSprites;
    private static readonly Vector2[] DefaultTextureCoords = [new(0,0), new(1,0), new(1,1), new(0,1)];
    private readonly List<(int Id, PointShadowCache.CasterPose Pose)> PointShadowCasterBuffer = new();
    private readonly List<(int Id, PointShadowCache.LampPose Pose)> PointShadowLampBuffer = new();
    private bool _shadowFitWarned;
    private bool _pointShadowWarned;
    public SceneRenderPipeline(IGraphics2D graphics2D, IGraphics3D graphics3D,
        ITextureFactory textureFactory, IModelFactory? modelFactory,
        IVertexArrayFactory? vertexArrayFactory = null, IVertexBufferFactory? vertexBufferFactory = null,
        IIndexBufferFactory? indexBufferFactory = null)
    {
        this.graphics2D = graphics2D; this.graphics3D = graphics3D; this.textureFactory = textureFactory;
        _builder = new(textureFactory, modelFactory);
        _meshPass = new(graphics3D, _state.StaticMeshes, vertexArrayFactory, vertexBufferFactory, indexBufferFactory);
    }

    public void RenderScene(Context context, in SceneView view)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_state.Activate(context)) _builder.Release();
        if (graphics3D.ActivateScene(context)) _state.PointShadows.MarkStale();
        _state.StaticMeshes.BeginFrame();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var start = Stopwatch.GetTimestamp();
        try
        {
            var buildStart = Stopwatch.GetTimestamp();
            var data = _builder.Build(context, view);
            var prepareMs = Stopwatch.GetElapsedTime(buildStart).TotalMilliseconds;
            graphics3D.DrawSkybox(view.SkyViewProjection);
            RenderSpritesAndSubTextures(context, graphics2D, textureFactory, view);
            Render3D(data, view);
            _state.StaticMeshes.CommitMergeWorldHistory();
            _state.StaticMeshes.EvictStaleCombinedMeshes();
            var stats = graphics3D.GetStats();
            stats.PrepareCpuMs = prepareMs;
            stats.SceneCpuMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            stats.SceneAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
        }
        finally
        {
            _builder.ReleaseFrame();
            _state.StaticMeshes.AbortFrame();
            SpriteDrawList.Clear();
            PointShadowCasterBuffer.Clear();
            PointShadowLampBuffer.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _state.Dispose(); _builder.Release();
        SpriteDrawList.Clear(); PointShadowCasterBuffer.Clear(); PointShadowLampBuffer.Clear();
        graphics3D.ReleaseSceneResources();
    }
    private struct OrderedSprite
    {
        public float Order;
        public int EntityId;
        public byte Kind;
        public Matrix4x4 Transform;
        public Vector4 Color;
        public float Tiling;
        public string? Path;
        public Vector2[]? TexCoords;
    }

    private static int CompareSprites(OrderedSprite a, OrderedSprite b)
    {
        var order = a.Order.CompareTo(b.Order);
        return order != 0 ? order : a.EntityId.CompareTo(b.EntityId);
    }

    private void RenderSpritesAndSubTextures(
        Context context,
        IGraphics2D graphics2D,
        ITextureFactory? textureFactory,
        in SceneView view)
    {
        graphics2D.BeginScene(view);
        CollectSprites(context, textureFactory);
        SpriteDrawList.Sort(SpriteDrawOrder);
        for (var i = 0; i < SpriteDrawList.Count; i++)
        {
            var draw = SpriteDrawList[i];
            if (draw.Kind == 0)
            {
                if (draw.Path != null && textureFactory != null)
                    graphics2D.DrawQuad(draw.Transform, textureFactory.Create(draw.Path), DefaultTextureCoords,
                        draw.Tiling, draw.Color, draw.EntityId);
                else
                    graphics2D.DrawQuad(draw.Transform, draw.Color, draw.EntityId);
            }
            else if (draw.TexCoords != null && textureFactory != null && draw.Path != null)
            {
                graphics2D.DrawQuad(draw.Transform, textureFactory.Create(draw.Path), draw.TexCoords, 1.0f,
                    draw.Color, draw.EntityId);
            }
        }

        graphics2D.EndScene();
    }

    private void CollectSprites(Context context, ITextureFactory? textureFactory)
    {
        SpriteDrawList.Clear();
        foreach (var (entity, sprite, transformComponent) in
                 context.View<SpriteRendererComponent, TransformComponent>())
        {
            if (SceneRenderDataBuilder.ShouldSkipForEntityVisibility(entity) || sprite.Color.W <= 0f)
                continue;

            var transform = transformComponent.GetWorldTransform();
            string? path = null;
            if (!string.IsNullOrWhiteSpace(sprite.TexturePath) && textureFactory != null)
            {
                try
                {
                    path = GetResolvedSpriteTexturePath(sprite);
                    textureFactory.Create(path);
                }
                catch (Exception ex)
                {
                    path = null;
                    if (WarnedFailedSprites.Add(sprite.TexturePath ?? ""))
                        Logger.Warning(ex, "Failed to load sprite texture '{TexturePath}' — drawing a solid color quad instead",
                            sprite.TexturePath);
                }
            }

            SpriteDrawList.Add(new OrderedSprite
            {
                Order = sprite.SortOrder,
                EntityId = entity.Id,
                Kind = 0,
                Transform = transform,
                Color = sprite.Color,
                Tiling = sprite.TilingFactor,
                Path = path
            });
        }

        foreach (var (entity, sub, transformComponent) in
                 context.View<SubTextureRendererComponent, TransformComponent>())
        {
            if (SceneRenderDataBuilder.ShouldSkipForEntityVisibility(entity) || sub.Color.W <= 0f)
                continue;
            if (textureFactory == null || string.IsNullOrWhiteSpace(sub.TexturePath))
                continue;

            try
            {
                var resolved = PathBuilder.Resolve(sub.TexturePath);
                var texture = textureFactory.Create(resolved);
                var texCoords = GetSubTextureTexCoords(sub, texture);
                SpriteDrawList.Add(new OrderedSprite
                {
                    Order = sub.SortOrder,
                    EntityId = entity.Id,
                    Kind = 1,
                    Transform = transformComponent.GetWorldTransform(),
                    Color = sub.Color,
                    Tiling = 1f,
                    Path = resolved,
                    TexCoords = texCoords
                });
            }
            catch (Exception ex)
            {
                if (WarnedFailedSprites.Add(sub.TexturePath))
                    Logger.Warning(ex, "Failed to load subtexture '{TexturePath}' — skipping sprite", sub.TexturePath);
            }
        }
    }
    
    private void Render3D(RenderSceneData data, in SceneView view)
    {
        var perf = new Render3DPerfFrame();

        var (ambientColor, ambientStrength) = data.Ambient;
        graphics3D.SetAmbientLight(ambientColor, ambientStrength);
        
        var (lightDirection, lightColor) = data.Directional;
        graphics3D.SetDirectionalLight(lightDirection, lightColor);
        
        var pointCount = data.PointCount;
        graphics3D.SetPointLights(data.PointLights.AsSpan(0, pointCount));
        perf.PointLights = pointCount;
        for (var i = 0; i < pointCount; i++)
        {
            if (data.PointLights[i].CastsShadow)
                perf.PointShadowLights++;
        }

        var shadowCasterMax = view.DirectionalShadowCasterMaxDistance;
        var fitDistance = graphics3D.ShadowQuality.Distance;
        if (shadowCasterMax > 0f)
            shadowCasterMax = System.Math.Min(shadowCasterMax, fitDistance);
        var shadowCasterMaxSq = shadowCasterMax > 0f ? shadowCasterMax * shadowCasterMax : 0f;
        var shadowCasterView = view.ViewPosition;
        var sunResolution = graphics3D.ShadowQuality.DirectionalResolution;
        var cascades = graphics3D.ShadowQuality.Cascades;

        graphics3D.SetDirectionalShadow(Matrix4x4.Identity, false);
        if (view.DirectionalShadows &&
            lightColor != Vector3.Zero &&
            LightingMath.TryFitDirectionalShadow(
                view.ViewProjection, lightDirection, out var lightViewProjection, fitDistance, sunResolution))
        {
            perf.DirectionalShadow = true;
            var nearSplit = fitDistance * 0.25f;
            Matrix4x4 nearProjection = default;
            var hasNear = cascades > 1 && nearSplit >= 1f &&
                          LightingMath.TryFitDirectionalShadow(
                              view.ViewProjection, lightDirection, out nearProjection, nearSplit, sunResolution);
            var near = hasNear ? nearProjection : lightViewProjection;
            graphics3D.BeginShadowCascade(near, 0);
            try
            {
                perf.DirectionalShadowPass = _meshPass.DrawOpaque3D(
                    data, near, view,
                    shadowCasterMaxSq, shadowCasterView);
            }
            finally
            {
                graphics3D.EndShadowPass();
            }

            if (hasNear)
            {
                graphics3D.BeginShadowCascade(lightViewProjection, 1);
                try
                {
                    perf.DirectionalShadowPass = Accumulate(perf.DirectionalShadowPass, _meshPass.DrawOpaque3D(
                        data, lightViewProjection, view,
                        shadowCasterMaxSq, shadowCasterView));
                }
                finally
                {
                    graphics3D.EndShadowPass();
                }
            }

            graphics3D.SetDirectionalShadow(near, true);
            graphics3D.SetFarDirectionalShadow(lightViewProjection);
        }
        else if (view.DirectionalShadows && lightColor != Vector3.Zero && !_shadowFitWarned)
        {
            _shadowFitWarned = true;
            Logger.Warning("Directional shadow fit failed; drawing the frame without directional shadows");
        }

        if (!view.PointShadows)
        {
            _state.PointShadows.MarkStale();
        }
        else
        {
            PointShadowCasterBuffer.Clear();
            PointShadowCasterBuffer.AddRange(data.Casters);
            perf.PointShadowCasters = PointShadowCasterBuffer.Count;
            PointShadowLampBuffer.Clear();
            for (var li = 0; li < pointCount; li++)
            {
                var resolved = data.PointLights[li];
                if (!resolved.CastsShadow)
                    continue;
                PointShadowLampBuffer.Add((resolved.EntityId, new PointShadowCache.LampPose(resolved.Position, resolved.Range)));
            }

            Span<Matrix4x4> pointFaces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
            for (var i = 0; i < pointCount; i++)
            {
                var light = data.PointLights[i];
                if (!light.CastsShadow)
                    continue;

                var dirty = _state.PointShadows.NeedsRedraw(
                    light.EntityId, light.Position, light.Range, PointShadowCasterBuffer);
                var near = Vector3.Distance(view.ViewPosition, light.Position) <= LightingMath.PointShadowDistance;
                if (!dirty)
                {
                    if (near)
                        _state.PointShadows.RememberClean(light.EntityId);
                    if (near && !graphics3D.UseCachedPointShadow(i, light.EntityId))
                        dirty = true;
                    else
                    {
                        perf.PointShadowLightsCacheHit++;
                        continue;
                    }
                }

                _state.PointShadows.RememberDirty(light.EntityId);
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
                            _meshPass.DrawOpaque3D(data, pointFaces[face], view,
                                shadowCasterMaxSq, shadowCasterView, light.Position));
                    }
                    finally
                    {
                        graphics3D.EndPointShadowFace();
                    }
                }

                if (drew)
                    _state.PointShadows.RememberClean(light.EntityId);
            }

            _state.PointShadows.Replace(PointShadowCasterBuffer, PointShadowLampBuffer);
        }

        graphics3D.BeginScene(view);
        try
        {
            perf.ColorPass = _meshPass.DrawOpaque3D(data, view.ViewProjection, view);
        }
        finally
        {
            graphics3D.EndScene();
        }

        perf.GpuDrawCalls = graphics3D.GetStats().DrawCalls;
        ApplyPipelineStats(graphics3D.GetStats(), in perf);
    }
    private static void ApplyPipelineStats(Statistics dest, in Render3DPerfFrame perf)
    {
        var color = perf.ColorPass;
        dest.Renderers = color.Renderers;
        dest.FrustumCulled = color.Culled;
        dest.ZoneCulled = color.ZoneCulled;
        dest.ShadowCasterCulled =
            perf.DirectionalShadowPass.ShadowCasterCulled + perf.PointShadowPass.ShadowCasterCulled;
        dest.SingleMaterialDraws = color.SingleMaterialDraws;
        dest.MultiMaterialDraws = color.MultiMaterialDraws;
        dest.MaxBatchInstances = color.MaxBatchInstances;
        dest.MergedGroups = color.MergedGroups;
        dest.MergedVertices = color.MergedVertices;
        dest.MergedRebuilds = color.MergedRebuilds
                                + perf.DirectionalShadowPass.MergedRebuilds
                                + perf.PointShadowPass.MergedRebuilds;
        dest.PointLights = perf.PointLights;
        dest.PointShadowLights = perf.PointShadowLights;
        dest.PointShadowCacheHits = perf.PointShadowLightsCacheHit;
        dest.DirectionalShadow = perf.DirectionalShadow;
        dest.ColorCpuMs = color.CpuMs;
        dest.ColorCollectCpuMs = color.CollectCpuMs;
        dest.ColorMeshFlushCpuMs = color.MeshFlushCpuMs;
        dest.ColorMergeFlushCpuMs = color.MergeFlushCpuMs;
        dest.ShadowCpuMs = perf.DirectionalShadowPass.CpuMs + perf.PointShadowPass.CpuMs;
    }

    private struct Render3DPerfFrame
    {
        public bool DirectionalShadow;
        public MeshPassStats DirectionalShadowPass;
        public MeshPassStats ColorPass;
        public MeshPassStats PointShadowPass;
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

    private static MeshPassStats Accumulate(MeshPassStats total, MeshPassStats pass)
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
        total.MergedGroups += pass.MergedGroups;
        total.MergedVertices += pass.MergedVertices;
        total.MergedRebuilds += pass.MergedRebuilds;
        total.Vertices += pass.Vertices;
        total.Indices += pass.Indices;
        total.CpuMs += pass.CpuMs;
        total.CollectCpuMs += pass.CollectCpuMs;
        total.MeshFlushCpuMs += pass.MeshFlushCpuMs;
        total.MergeFlushCpuMs += pass.MergeFlushCpuMs;
        return total;
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
