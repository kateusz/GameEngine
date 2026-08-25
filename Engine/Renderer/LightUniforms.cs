using System.Numerics;

namespace Engine.Renderer;

public readonly record struct PointLightUniform(
    Vector3 Position,
    Vector3 Color,
    float Constant,
    float Linear,
    float Quadratic);

public readonly record struct SpotLightUniform(
    Vector3 Position,
    Vector3 Direction,
    Vector3 Color,
    float Constant,
    float Linear,
    float Quadratic,
    float InnerCutoffCos,
    float OuterCutoffCos);
