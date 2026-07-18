using System.Numerics;
using ArenaShooter.assets.scripts;
using Audio;
using ECS;
using Engine.Scene.Systems;
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
    public void FacingToward_PointsFromPlayerToMouse()
    {
        var facing = ArenaSystem.FacingToward(Vector2.Zero, new Vector2(0f, 4f), new Vector2(1f, 0f));

        facing.X.ShouldBe(0f, 0.0001f);
        facing.Y.ShouldBe(1f, 0.0001f);
    }

    [Fact]
    public void FacingToward_Coincident_KeepsCurrent()
    {
        var current = new Vector2(0f, -1f);
        ArenaSystem.FacingToward(Vector2.One, Vector2.One, current).ShouldBe(current);
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
    public void OnUpdate_MouseAim_UpdatesFacingTowardWorldPoint()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(50f, 50f));
        mouse.IsButtonDown(MouseButtons.Left).Returns(false);

        var cameras = Substitute.For<ICameraQueries>();
        cameras.ScreenToWorld2D(Arg.Any<Vector2>()).Returns(new Vector2(0f, 3f));

        var physics = Substitute.For<IPhysicsQueries>();
        var (system, game) = CreateSystem(keyboard, mouse, cameras, physics);
        game.Facing = new Vector2(1f, 0f);

        system.OnUpdate(TimeSpan.Zero);

        game.Facing.X.ShouldBe(0f, 0.0001f);
        game.Facing.Y.ShouldBe(1f, 0.0001f);
    }

    [Fact]
    public void OnUpdate_NullWorldMouse_KeepsFacing()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(50f, 50f));

        var cameras = Substitute.For<ICameraQueries>();
        cameras.ScreenToWorld2D(Arg.Any<Vector2>()).Returns((Vector2?)null);

        var physics = Substitute.For<IPhysicsQueries>();
        var (system, game) = CreateSystem(keyboard, mouse, cameras, physics);
        game.Facing = new Vector2(0f, -1f);

        system.OnUpdate(TimeSpan.Zero);

        game.Facing.ShouldBe(new Vector2(0f, -1f));
    }

    [Fact]
    public void OnUpdate_LmbHeld_RaycastHitsEnemy_KillsItAndScores()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(50f, 50f));
        mouse.IsButtonDown(MouseButtons.Left).Returns(true);

        var cameras = Substitute.For<ICameraQueries>();
        cameras.ScreenToWorld2D(Arg.Any<Vector2>()).Returns(new Vector2(5f, 0f));

        var context = new Context();
        var game = new ArenaGameComponent { SpawnTimer = 100f, Facing = new Vector2(1f, 0f) };
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

        var contacts = new PhysicsContactQueue();
        var audioPlayback = Substitute.For<IAudioPlayback>();
        var audio = Substitute.For<IAudio>();
        var system = new ArenaSystem(context, keyboard, mouse, cameras, physics, contacts, audioPlayback, audio);
        system.OnUpdate(TimeSpan.Zero);

        enemyComponent.Alive.ShouldBeFalse();
        game.Score.ShouldBe(1);
    }

    [Fact]
    public void OnUpdate_NoLmb_DoesNotShoot()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(50f, 50f));
        mouse.IsButtonDown(MouseButtons.Left).Returns(false);

        var cameras = Substitute.For<ICameraQueries>();
        cameras.ScreenToWorld2D(Arg.Any<Vector2>()).Returns(new Vector2(5f, 0f));

        var physics = Substitute.For<IPhysicsQueries>();
        var (system, _) = CreateSystem(keyboard, mouse, cameras, physics);

        system.OnUpdate(TimeSpan.Zero);

        physics.DidNotReceive().Raycast(
            Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Entity?>(), Arg.Any<bool>());
    }

    [Fact]
    public void OnUpdate_OutsideSurface_DoesNotShoot()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        var mouse = Substitute.For<IMouseInput>();
        mouse.Position.Returns(new Vector2(50f, 50f));
        mouse.IsButtonDown(MouseButtons.Left).Returns(true);

        var cameras = Substitute.For<ICameraQueries>();
        cameras.ScreenToWorld2D(Arg.Any<Vector2>()).Returns((Vector2?)null);

        var physics = Substitute.For<IPhysicsQueries>();
        var (system, _) = CreateSystem(keyboard, mouse, cameras, physics);

        system.OnUpdate(TimeSpan.Zero);

        physics.DidNotReceive().Raycast(
            Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Entity?>(), Arg.Any<bool>());
    }

    private static (ArenaSystem System, ArenaGameComponent Game) CreateSystem(
        IKeyboardInput keyboard,
        IMouseInput mouse,
        ICameraQueries cameras,
        IPhysicsQueries physics)
    {
        var context = new Context();
        var game = new ArenaGameComponent { SpawnTimer = 100f };
        Register(context, 1, "Game", game);
        Register(context, 2, "Player",
            new TransformComponent(),
            new RigidBody2DComponent { BodyType = RigidBodyType.Dynamic },
            new SpriteRendererComponent());
        
        var contacts = new PhysicsContactQueue();
        var audioPlayback = Substitute.For<IAudioPlayback>();
        var audio = Substitute.For<IAudio>();
        return (new ArenaSystem(context, keyboard, mouse, cameras, physics, contacts, audioPlayback, audio), game);
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
