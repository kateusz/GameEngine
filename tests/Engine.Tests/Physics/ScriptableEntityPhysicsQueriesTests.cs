using System.Numerics;
using Audio;
using ECS;
using NSubstitute;
using Scripting;
using Shouldly;

namespace Engine.Tests.Physics;

public class ScriptableEntityPhysicsQueriesTests
{
    [Fact]
    public void Raycast_ForwardsIgnoreSelf()
    {
        var queries = Substitute.For<IPhysicsQueries>();
        var script = new QueryForwardingScript(queries);
        var self = Entity.Create(7, "Self");
        script.SetEntity(self);

        script.FireRaycast();

        queries.Received(1).Raycast(
            Vector2.Zero,
            Vector2.UnitY,
            10f,
            self,
            false);
    }

    private sealed class QueryForwardingScript : ScriptableEntity
    {
        public QueryForwardingScript(IPhysicsQueries physicsQueries)
            : base(new ComponentAccessor(), Substitute.For<IAudio>(), Substitute.For<IAudioPlayback>(), physicsQueries)
        {
        }

        public void FireRaycast() => Raycast(Vector2.Zero, Vector2.UnitY, 10f);
    }
}
