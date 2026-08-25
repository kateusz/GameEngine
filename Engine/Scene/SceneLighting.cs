using System.Numerics;

namespace Engine.Scene;

internal readonly record struct SceneLighting(
    Vector3 AmbientColor,
    float AmbientStrength,
    Vector3 DirectionalDirection,
    Vector3 DirectionalColor,
    Matrix4x4? ShadowLightSpace = null)
{
    public static SceneLighting Default { get; } = new(
        Vector3.One,
        0.1f,
        new Vector3(0, -1, 0),
        Vector3.Zero);
}
