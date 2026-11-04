using System.Numerics;

namespace Engine.Renderer.Pipeline;

public readonly record struct SceneView(
    Matrix4x4 ViewProjection,
    Vector3 ViewPosition = default,
    bool PointShadows = true,
    float DirectionalShadowCasterMaxDistance = LightingMath.ShadowDistance,
    bool DirectionalShadows = true,
    Matrix4x4 SkyViewProjection = default,
    Matrix4x4 View = default,
    Matrix4x4 Projection = default,
    uint TargetWidth = 0,
    uint TargetHeight = 0,
    bool Ssao = false,
    float SsaoRadius = 0.5f,
    float SsaoStrength = 1f);
