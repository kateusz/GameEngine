using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Buffers.FrameBuffer;
using Engine.Renderer.Pipeline;
using Serilog;

namespace Engine.Scene;

/// <summary>
/// Owns shadow GPU maps and runs directional/point depth passes via MeshPassRenderer.
/// Graphics3D only binds targets and draws depth.
/// </summary>
internal sealed class ShadowRenderer : IDisposable
{
    private static readonly ILogger Logger = Log.ForContext<ShadowRenderer>();

    private readonly IGraphics3D _graphics;
    private readonly MeshPassRenderer _meshPass;
    private readonly PointShadowCache _cache;
    private readonly ShadowMapStore? _maps;
    private readonly List<(int Id, PointShadowCache.CasterPose Pose)> _casters = new();
    private readonly List<(int Id, PointShadowCache.LampPose Pose)> _lamps = new();
    private bool _shadowFitWarned;
    private bool _pointShadowWarned;
    private bool _disposed;

    internal ShadowRenderer(
        IGraphics3D graphics,
        MeshPassRenderer meshPass,
        PointShadowCache cache,
        IFrameBufferFactory? frameBuffers = null)
    {
        _graphics = graphics;
        _meshPass = meshPass;
        _cache = cache;
        // Unit recorders omit the factory; Graphics3D then uses its fallback store.
        _maps = frameBuffers is null ? null : new ShadowMapStore(frameBuffers);
    }

    /// <summary>Bind pipeline maps for the whole 3D frame (shadows + color). Call <see cref="Unbind"/> after color.</summary>
    internal void Bind()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_maps is not null)
            _graphics.BindShadowMapStore(_maps);
    }

    internal void Unbind()
    {
        if (_maps is not null)
            _graphics.BindShadowMapStore(null);
    }

    internal void Render(RenderSceneData data, in SceneView view, ref RenderShadowStats perf)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var (lightDirection, lightColor) = data.Directional;
        var pointCount = data.PointCount;

        var shadowCasterMax = view.DirectionalShadowCasterMaxDistance;
        var fitDistance = _graphics.ShadowQuality.Distance;
        if (shadowCasterMax > 0f)
            shadowCasterMax = System.Math.Min(shadowCasterMax, fitDistance);
        var shadowCasterMaxSq = shadowCasterMax > 0f ? shadowCasterMax * shadowCasterMax : 0f;
        var shadowCasterView = view.ViewPosition;
        var sunResolution = _graphics.ShadowQuality.DirectionalResolution;
        var cascades = _graphics.ShadowQuality.Cascades;

        _graphics.SetDirectionalShadow(Matrix4x4.Identity, false);
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
            _graphics.BeginShadowCascade(near, 0);
            try
            {
                perf.DirectionalPass = Accumulate(perf.DirectionalPass, _meshPass.DrawOpaque3D(
                    data, near, view, shadowCasterMaxSq, shadowCasterView));
            }
            finally
            {
                _graphics.EndShadowPass();
            }

            if (hasNear)
            {
                _graphics.BeginShadowCascade(lightViewProjection, 1);
                try
                {
                    perf.DirectionalPass = Accumulate(perf.DirectionalPass, _meshPass.DrawOpaque3D(
                        data, lightViewProjection, view, shadowCasterMaxSq, shadowCasterView));
                }
                finally
                {
                    _graphics.EndShadowPass();
                }
            }

            _graphics.SetDirectionalShadow(near, true);
            _graphics.SetFarDirectionalShadow(lightViewProjection);
        }
        else if (view.DirectionalShadows && lightColor != Vector3.Zero && !_shadowFitWarned)
        {
            _shadowFitWarned = true;
            Logger.Warning("Directional shadow fit failed; drawing the frame without directional shadows");
        }

        if (!view.PointShadows)
        {
            _cache.MarkStale();
            return;
        }

        _casters.Clear();
        _casters.AddRange(data.Casters);
        perf.PointShadowCasters = _casters.Count;
        _lamps.Clear();
        for (var li = 0; li < pointCount; li++)
        {
            var resolved = data.PointLights[li];
            if (!resolved.CastsShadow)
                continue;
            _lamps.Add((resolved.EntityId, new PointShadowCache.LampPose(resolved.Position, resolved.Range)));
        }

        Span<Matrix4x4> pointFaces = stackalloc Matrix4x4[LightingMath.PointShadowFaceCount];
        for (var i = 0; i < pointCount; i++)
        {
            var light = data.PointLights[i];
            if (!light.CastsShadow)
                continue;

            var dirty = _cache.NeedsRedraw(light.EntityId, light.Position, light.Range, _casters);
            var near = Vector3.Distance(view.ViewPosition, light.Position) <= LightingMath.PointShadowDistance;
            if (!dirty)
            {
                if (near)
                    _cache.RememberClean(light.EntityId);
                if (near && !_graphics.UseCachedPointShadow(i, light.EntityId))
                    dirty = true;
                else
                {
                    perf.PointShadowLightsCacheHit++;
                    continue;
                }
            }

            _cache.RememberDirty(light.EntityId);
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
                if (!_graphics.BeginPointShadowFace(
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
                    perf.PointPass = Accumulate(perf.PointPass, _meshPass.DrawOpaque3D(
                        data, pointFaces[face], view, shadowCasterMaxSq, shadowCasterView, light.Position));
                }
                finally
                {
                    _graphics.EndPointShadowFace();
                }
            }

            if (drew)
                _cache.RememberClean(light.EntityId);
        }

        _cache.Replace(_casters, _lamps);
    }

    internal void OnSceneChanged()
    {
        // Maps die with the scene; cache must not claim clean maps that no longer exist.
        _maps?.Clear();
        _cache.Clear();
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

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _maps?.Dispose();
        _casters.Clear();
        _lamps.Clear();
    }
}

internal struct RenderShadowStats
{
    public bool DirectionalShadow;
    public MeshPassStats DirectionalPass;
    public MeshPassStats PointPass;
    public int PointShadowCasters;
    public int PointShadowLightsCacheHit;
    public int PointShadowLightsTooFar;
    public int PointShadowLightsRedrawn;
    public int PointShadowFaces;
    public int PointShadowOpaque3D;
}
