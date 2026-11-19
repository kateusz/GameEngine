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
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Serilog;

namespace Engine.Scene;

internal sealed class SceneRenderDataBuilder(ITextureFactory textureFactory, IModelFactory? modelFactory)
{
    private static readonly ILogger Logger = Log.ForContext<SceneRenderDataBuilder>();
    private readonly HashSet<string> WarnedFailedModels = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ResolvedFrameModel> FrameModels = new(StringComparer.Ordinal);
    private readonly record struct ResolvedFrameModel(string Path, Model? Model);
    private readonly List<int> ActiveVisibilityZoneEntityIds = new();
    private readonly HashSet<int> WarnedMissingVisibilityZoneEntityIds = new();
    private bool _visibilityZonesEnabled;
    private readonly RenderSceneData _data = new();
    private enum OpaqueSurfaceKind { UnitCube, SingleSubmesh, AllSubmeshes }
    internal readonly record struct PbrFactors(float Metallic, float Roughness, float Ao);
    internal RenderSceneData Build(Context context, in SceneView view)
    {
        _data.Clear(); FrameModels.Clear();
        try
        {
            PrepareActiveVisibilityZones(context, view.ViewPosition);
            _data.Ambient = ResolveAmbient(context);
            _data.Directional = ResolveDirectional(context);
            _data.PointCount = ResolvePointLights(context, _data.PointLights);
            foreach (var (entity, renderer, transform) in context.View<ModelRendererComponent, TransformComponent>())
            {
                var world = ModelMeshPivot.ToDrawTransform(transform.GetWorldTransform(), renderer.Pivot);
                var visible = !ShouldSkipForEntityVisibility(entity);
                var stats = new MeshPassStats();
                var zoneCulled = visible && ShouldSkipForVisibilityZone(renderer.VisibilityZoneEntityId, context, ref stats);
                var valid = false; var kind = default(OpaqueSurfaceKind); Model? model = null;
                var submeshIndex = -1; var pose = default(PointShadowCache.CasterPose);
                if (visible) valid = TryResolveOpaqueSurface(renderer, world, modelFactory, out pose, out kind, out model, out submeshIndex);
                var missingIndex = !valid && model != null && renderer.MeshIndex is int index && (index < 0 || index >= model.Submeshes.Count);
                var moving = entity.TryGetComponent<RigidBody2DComponent>(out var body) && body.BodyType is RigidBodyType.Dynamic or RigidBodyType.Kinematic;
                var start = _data.Surfaces.Count;
                if (valid && kind != OpaqueSurfaceKind.UnitCube)
                {
                    if (kind == OpaqueSurfaceKind.SingleSubmesh) AddSurface(model!.Submeshes[submeshIndex], renderer, world);
                    else foreach (var mesh in model!.Submeshes) AddSurface(mesh, renderer, world);
                }
                Texture2D? texture = null; var textureFailed = false;
                if (valid && kind == OpaqueSurfaceKind.UnitCube)
                {
                    if (!string.IsNullOrWhiteSpace(renderer.ModelPath))
                    {
                        var resolved = ResolveFrameModel(renderer.ModelPath, modelFactory!).Path;
                        if (WarnedFailedModels.Add(resolved)) Logger.Warning("Failed to load model assetPath={ModelPath} resolved={ResolvedPath} — drawing unit cube instead", renderer.ModelPath, resolved);
                    }
                    if (!string.IsNullOrWhiteSpace(renderer.TexturePath))
                    {
                        try { texture = textureFactory.Create(PathBuilder.Resolve(renderer.TexturePath), sRgb: true); }
                        catch (Exception ex) { textureFailed = true; Logger.Warning(ex, "Failed to load cube texture {TexturePath}", renderer.TexturePath); }
                    }
                }
                _data.Objects.Add(new(entity.Id, world, visible, zoneCulled, valid, missingIndex, moving,
                    kind == OpaqueSurfaceKind.UnitCube, renderer.Color, ResolvePbr(renderer), CombineEmissive(Vector3.Zero, renderer),
                    renderer.LodBias, texture, textureFailed, renderer.TilingFactor, start, _data.Surfaces.Count - start));
                if (visible && valid) _data.Casters.Add((entity.Id, pose));
            }
            return _data;
        }
        catch { _data.Clear(); throw; }
        finally { FrameModels.Clear(); }
    }

    private void AddSurface(Mesh mesh, ModelRendererComponent renderer, Matrix4x4 world)
    {
        var pbr = ResolveEffectivePbr(renderer, mesh);
        Aabb? worldBounds = null;
        if (mesh.Bounds is { } local)
        {
            Aabb.WorldMinMax(world, local, out var min, out var max);
            if (IsFinite(min) && IsFinite(max))
                worldBounds = new Aabb(min, max);
        }
        _data.Surfaces.Add(new(mesh, pbr, CombineEmissive(mesh.EmissiveFactor, renderer), worldBounds));
    }

    private static bool IsFinite(Vector3 v) =>
        float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z);

    internal static PbrFactors ResolveEffectivePbr(ModelRendererComponent renderer, Mesh mesh)
    {
        // Compatibility with old unpacked children: use file defaults without editing the scene.
        if (renderer.MeshIndex is int && !renderer.FactorsSeeded && renderer.Metallic == 0f && renderer.Roughness == 0.5f && renderer.Ao == 1f)
            return new(Finite01(mesh.MetallicFactor), Finite01(mesh.RoughnessFactor), Finite01(renderer.Ao));
        return ResolvePbr(renderer);
    }

    internal void Release() { _data.Clear(); FrameModels.Clear(); WarnedFailedModels.Clear(); WarnedMissingVisibilityZoneEntityIds.Clear(); }
    internal void ReleaseFrame() => _data.Clear();
    private ResolvedFrameModel ResolveFrameModel(string assetPath, IModelFactory modelFactory)
    {
        if (FrameModels.TryGetValue(assetPath, out var resolved))
            return resolved;

        var path = PathBuilder.Resolve(assetPath);
        resolved = new ResolvedFrameModel(path, modelFactory.Create(path));
        FrameModels.Add(assetPath, resolved);
        return resolved;
    }

    private bool TryResolveOpaqueSurface(
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

        model = ResolveFrameModel(modelRenderer.ModelPath, modelFactory).Model;
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

    private void PrepareActiveVisibilityZones(Context context, Vector3 cameraPosition)
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

    internal static bool ShouldSkipForEntityVisibility(Entity entity) =>
        entity.TryGetComponent<TransformComponent>(out var transform)
        && !transform.EffectiveVisible;

    private bool ShouldSkipForVisibilityZone(int visibilityZoneEntityId, Context context, ref MeshPassStats stats)
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

    internal static (Vector3 Color, float Strength) ResolveAmbient(Context context)
    {
        foreach (var (_, alc) in context.View<AmbientLightComponent>())
            return (new Vector3(alc.Color.X, alc.Color.Y, alc.Color.Z), alc.Strength);

        return (Vector3.One, 0.1f);
    }

    internal static (Vector3 Direction, Vector3 Color) ResolveDirectional(Context context)
    {
        foreach (var (_, dlc) in context.View<DirectionalLightComponent>())
            return (
                LightingMath.NormalizeDirection(dlc.Direction),
                new Vector3(dlc.Color.X, dlc.Color.Y, dlc.Color.Z) * MathF.Max(0f, dlc.Intensity));

        return (LightingMath.DefaultDirection, Vector3.Zero);
    }

    internal static PbrFactors ResolvePbr(ModelRendererComponent renderer) =>
        new(Finite01(renderer.Metallic), Finite01(renderer.Roughness), Finite01(renderer.Ao));

    internal static Vector3 CombineEmissive(Vector3 fileFactor, ModelRendererComponent renderer)
    {
        var sum = fileFactor + renderer.Emissive * renderer.EmissiveStrength;
        return new Vector3(
            LightingMath.ClampEmissive(sum.X),
            LightingMath.ClampEmissive(sum.Y),
            LightingMath.ClampEmissive(sum.Z));
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
                transform.GetWorldTransform().Translation + (light.ApplyOffset ? light.Offset : default),
                new Vector3(light.Color.X, light.Color.Y, light.Color.Z),
                MathF.Max(0f, light.Intensity),
                light.Range,
                light.CastsShadow,
                entity.Id);
        }

        return count;
    }
}
