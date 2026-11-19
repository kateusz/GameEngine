using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;

namespace Engine.Scene;

// Borrowed GPU assets are valid through the synchronous frame. No ECS/component references escape the builder.
internal sealed class RenderSceneData
{
    internal readonly List<RenderObjectData> Objects = new();
    internal readonly List<SurfaceDrawData> Surfaces = new();
    internal readonly List<(int Id, PointShadowCache.CasterPose Pose)> Casters = new();
    internal readonly PointLightData[] PointLights = new PointLightData[LightingMath.MaxPointLights];
    internal int PointCount;
    internal (Vector3 Color, float Strength) Ambient;
    internal (Vector3 Direction, Vector3 Color) Directional;

    internal void Clear() { Objects.Clear(); Surfaces.Clear(); Casters.Clear(); PointCount = 0; }
}

internal readonly record struct RenderObjectData(
    int EntityId, Matrix4x4 World, bool Visible, bool ZoneCulled, bool Valid, bool MissingMeshIndex,
    bool MovingBody, bool IsCube, Vector4 Tint, SceneRenderDataBuilder.PbrFactors Pbr, Vector3 Emissive,
    float LodBias, Texture2D? Texture, bool TextureFailed, float Tiling, int SurfaceStart, int SurfaceCount,
    Aabb? WorldBounds);

internal readonly record struct SurfaceDrawData(
    Mesh Mesh, SceneRenderDataBuilder.PbrFactors Pbr, Vector3 Emissive, Aabb? WorldBounds);
