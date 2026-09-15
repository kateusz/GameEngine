using ECS;
using Engine.Physics;
using Engine.Scene.Systems;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class PhysicsContactQueueTests
{
    [Fact]
    public void DrainContacts_ReturnsEnqueuedAndClears()
    {
        var queue = new PhysicsContactQueue();
        var a = Entity.Create(1, "a");
        var b = Entity.Create(2, "b");
        queue.Enqueue(new PhysicsContact(a, b, IsTrigger: false, IsBegin: true));

        var drained = queue.DrainContacts();
        drained.Length.ShouldBe(1);
        drained[0].Self.ShouldBe(a);
        drained[0].Other.ShouldBe(b);

        queue.DrainContacts().Length.ShouldBe(0);
    }

    [Fact]
    public void OnContactBegin_EnqueuesBothOrderings()
    {
        var queue = new PhysicsContactQueue();
        var a = Entity.Create(1, "a");
        var b = Entity.Create(2, "b");
        var bodyA = Substitute.For<IPhysicsBody2D>();
        var bodyB = Substitute.For<IPhysicsBody2D>();
        bodyA.Entity.Returns(a);
        bodyB.Entity.Returns(b);

        queue.OnContactBegin(bodyA, bodyB, isTrigger: true);

        var drained = queue.DrainContacts();
        drained.Length.ShouldBe(2);
        drained[0].ShouldBe(new PhysicsContact(a, b, IsTrigger: true, IsBegin: true));
        drained[1].ShouldBe(new PhysicsContact(b, a, IsTrigger: true, IsBegin: true));
    }
}
