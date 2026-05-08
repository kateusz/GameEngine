using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Camera;

public class CameraComponent : IComponent
{
    public CameraProjectionTypeData ProjectionType { get; set; } = CameraProjectionTypeData.Orthographic;
    public float OrthographicSize { get; set; } = 10.0f;
    public float OrthographicNear { get; set; } = -1.0f;
    public float OrthographicFar { get; set; } = 1.0f;
    public float PerspectiveFOV { get; set; } = MathF.PI / 4.0f;
    public float PerspectiveNear { get; set; } = 0.01f;
    public float PerspectiveFar { get; set; } = 1000.0f;
    public float AspectRatio { get; set; } = 16.0f / 9.0f;
    public bool Primary { get; set; }
    public bool FixedAspectRatio { get; set; }

    [JsonIgnore]
    public Matrix4x4? CameraViewTransform { get; set; }

    public IComponent Clone()
    {
        var cloned = new CameraComponent
        {
            Primary = Primary,
            FixedAspectRatio = FixedAspectRatio,
            ProjectionType = ProjectionType,
            OrthographicSize = OrthographicSize,
            OrthographicNear = OrthographicNear,
            OrthographicFar = OrthographicFar,
            PerspectiveFOV = PerspectiveFOV,
            PerspectiveNear = PerspectiveNear,
            PerspectiveFar = PerspectiveFar,
            AspectRatio = AspectRatio
        };

        return cloned;
    }
}