using ECS;
using Engine.Renderer.Cameras;

namespace Engine.Scene.Components;

public class CameraComponent : Component
{
    public Camera Camera { get; set; }
    public bool Primary { get; set; } = true; // TODO: think about moving to Scene
    public bool FixedAspectRatio { get; set; } = false;
}