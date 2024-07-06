using System.Numerics;
using ECS;

namespace Engine.Scene.Components;

public class TransformComponent : Component
{
    public Matrix4x4 Transform { get; set; } = Matrix4x4.Identity;
}