namespace Engine.Physics;

public interface IPhysicsContactListener3D
{
    void OnContactBegin(IPhysicsBody3D bodyA, IPhysicsBody3D bodyB, bool isTrigger);
    void OnContactEnd(IPhysicsBody3D bodyA, IPhysicsBody3D bodyB, bool isTrigger);
}
