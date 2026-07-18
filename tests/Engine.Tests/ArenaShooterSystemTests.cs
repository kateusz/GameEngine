using System.Numerics;
using ArenaShooter.assets.scripts;
using ECS;
using Input;
using NSubstitute;
using SceneComponents;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class ArenaShooterSystemTests
{
    [Fact]
    public void MoveVelocity_Diagonal_IsNormalizedToSpeed()
    {
        var v = ArenaSystem.MoveVelocity(up: true, down: false, left: false, right: true, speed: 5f);

        v.Length().ShouldBe(5f, 0.0001f);
        v.X.ShouldBe(v.Y, 0.0001f);
        v.X.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void MoveVelocity_NoKeysOrOpposingKeys_IsZero()
    {
        ArenaSystem.MoveVelocity(false, false, false, false, 5f).ShouldBe(Vector2.Zero);
        ArenaSystem.MoveVelocity(up: true, down: true, left: true, right: true, 5f).ShouldBe(Vector2.Zero);
    }

    [Fact]
    public void AimDirection_NoKeys_KeepsCurrentFacing()
    {
        var current = new Vector2(0f, -1f);

        ArenaSystem.AimDirection(false, false, false, false, current).ShouldBe(current);
    }

    [Fact]
    public void AimDirection_Up_ReturnsUnitUp()
    {
        var dir = ArenaSystem.AimDirection(up: true, false, false, false, new Vector2(1f, 0f));

        dir.X.ShouldBe(0f, 0.0001f);
        dir.Y.ShouldBe(1f, 0.0001f);
    }

    [Fact]
    public void EdgeSpawnPoint_AlwaysOnPerimeterWithinBounds()
    {
        var game = new ArenaGameComponent();
        var rng = new Random(1234);

        for (var i = 0; i < 200; i++)
        {
            var p = ArenaSystem.EdgeSpawnPoint(game, rng);

            p.X.ShouldBeInRange(game.MinX, game.MaxX);
            p.Y.ShouldBeInRange(game.MinY, game.MaxY);
            var onEdge = p.X == game.MinX || p.X == game.MaxX || p.Y == game.MinY || p.Y == game.MaxY;
            onEdge.ShouldBeTrue();
        }
    }

    [Fact]
    public void TracerGeometry_HorizontalShot_HasExpectedCenterAngleLength()
    {
        var (center, angle, length) = ArenaSystem.TracerGeometry(new Vector2(0f, 0f), new Vector2(2f, 0f));

        center.ShouldBe(new Vector2(1f, 0f));
        angle.ShouldBe(0f, 0.0001f);
        length.ShouldBe(2f, 0.0001f);
    }

    [Fact]
    public void OnUpdate_RaycastHitsEnemy_KillsItAndScores()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        keyboard.IsKeyDown(KeyCodes.Up).Returns(true); // arrow held -> aiming -> auto-fire

        var context = new Context();
        var game = new ArenaGameComponent { SpawnTimer = 100f }; // suppress spawns during the test
        Register(context, 1, "Game", game);
        Register(context, 2, "Player",
            new TransformComponent(),
            new RigidBody2DComponent { BodyType = RigidBodyType.Dynamic },
            new SpriteRendererComponent());

        var enemyComponent = new EnemyComponent { Alive = true };
        var enemy = Register(context, 3, "Enemy0",
            new TransformComponent(new Vector3(5f, 5f, 0f), Vector3.Zero, Vector3.One),
            new RigidBody2DComponent { BodyType = RigidBodyType.Kinematic },
            new SpriteRendererComponent(),
            enemyComponent);

        var physics = Substitute.For<IPhysicsQueries>();
        physics.Raycast(Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Entity?>(), Arg.Any<bool>())
            .Returns(new RaycastHit2D(enemy, new Vector2(5f, 5f), Vector2.Zero, 7.07f, false));

        var system = new ArenaSystem(context, keyboard, physics);
        system.OnUpdate(TimeSpan.Zero);

        enemyComponent.Alive.ShouldBeFalse();
        game.Score.ShouldBe(1);
    }

    private static Entity Register(Context context, int id, string name, params IComponent[] components)
    {
        var entity = Entity.Create(id, name);
        foreach (var component in components)
            entity.AddComponentDynamic(component);
        context.Register(entity);
        return entity;
    }
}
