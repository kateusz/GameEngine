using System.Numerics;

namespace Engine.Renderer.Pipeline;

public readonly record struct SceneView(
    Matrix4x4 ViewProjection,
    Vector3 ViewPosition = default,
    bool PointShadows = true,
    float DirectionalShadowCasterMaxDistance = LightingMath.ShadowDistance,
    bool DirectionalShadows = true,
    Matrix4x4 SkyViewProjection = default,
    float ViewportHeight = 0f,
    float FrustumHeightAtUnit = 0f,
    bool Orthographic = false,
    /// <summary>Projection aspect (width/height). Used by software occlusion; must not be derived from ViewProjection.</summary>
    float Aspect = 0f);
