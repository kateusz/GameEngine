using System.Numerics;

namespace Engine.Physics;

public interface IPhysicsWorld2DFactory
{
    IPhysicsWorld2D Create(Vector2 gravity);
    IPhysicsWorld3D Create3D(Vector3 gravity);
}
