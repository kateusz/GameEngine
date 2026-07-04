using System.Numerics;

namespace Engine.Physics;

public interface IPhysicsWorld2D : IDisposable
{
    void Step(float timeStep, int velocityIterations, int positionIterations);
    IPhysicsBody2D CreateBody(in PhysicsBodyDef def);
    void DestroyBody(IPhysicsBody2D body);
    void SetContactListener(IPhysicsContactListener? listener);
}
