using System.Numerics;

namespace Engine.Physics;

public interface IPhysicsWorld2DFactory
{
    IPhysicsWorld2D Create(Vector2 gravity);
}
