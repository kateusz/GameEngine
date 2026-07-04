using ECS;
using Engine.Scene.Systems;
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
}
