using System.Numerics;

namespace Engine.Physics;

public interface IPhysicsWorldFactory
{
    IPhysicsWorld2D Create(Vector2 gravity);
}
