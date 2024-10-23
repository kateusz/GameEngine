using ECS;

namespace Engine.Scene.Components;

public class CameraComponent : Component
{
    public SceneCamera Camera { get; set; } = new();
    public bool Primary { get; set; } = true; // TODO: think about moving to Scene
    public bool FixedAspectRatio { get; set; } = false;
}