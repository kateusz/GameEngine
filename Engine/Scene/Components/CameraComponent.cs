using ECS;

namespace Engine.Scene.Components;

public class CameraComponent : IComponent
{
    public SceneCamera Camera { get; set; } = new();
    public bool Primary { get; set; } = true; // TODO: think about moving to Scene
    public bool FixedAspectRatio { get; set; } = false;

    public IComponent Clone()
    {
        var cloned = new CameraComponent
        {
            Primary = Primary,
            FixedAspectRatio = FixedAspectRatio,
            Camera = new SceneCamera()
        };

        // Copy all camera properties
        cloned.Camera.ProjectionType = Camera.ProjectionType;
        cloned.Camera.OrthographicSize = Camera.OrthographicSize;
        cloned.Camera.OrthographicNear = Camera.OrthographicNear;
        cloned.Camera.OrthographicFar = Camera.OrthographicFar;
        cloned.Camera.PerspectiveFOV = Camera.PerspectiveFOV;
        cloned.Camera.PerspectiveNear = Camera.PerspectiveNear;
        cloned.Camera.PerspectiveFar = Camera.PerspectiveFar;
        cloned.Camera.AspectRatio = Camera.AspectRatio;

        return cloned;
    }
}