using Scripting;

namespace Engine.Physics;

public interface IPhysicsWorld3D : IPhysicsQueries3D, IDisposable
{
    void Step(float timeStep, int velocityIterations, int positionIterations);
    IPhysicsBody3D CreateBody(in PhysicsBodyDef3D def);
    void DestroyBody(IPhysicsBody3D body);
    void SetContactListener(IPhysicsContactListener3D? listener);
}
