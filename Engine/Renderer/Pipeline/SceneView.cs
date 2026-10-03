using System.Numerics;

namespace Engine.Renderer.Pipeline;

public readonly record struct SceneView(
    Matrix4x4 ViewProjection,
    Vector3 ViewPosition = default,
    bool PointShadows = true,
    float DirectionalShadowCasterMaxDistance = LightingMath.ShadowDistance,
    bool DirectionalShadows = true);
