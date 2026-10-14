using System.Numerics;
using Audio;
using CrashLike.assets.scripts;
using ECS;
using Input;
using NSubstitute;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class CrashLikeSystemTests
{
    [Fact]
    public void OnUpdate_RightKey_AcceleratesForward()
    {
        var world = CreateWorld();
        world.Keyboard.IsKeyDown(KeyCodes.D).Returns(true);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        world.Player.GetComponent<RigidBody2DComponent>().Velocity.X.ShouldBeGreaterThan(0.5f);
        world.Game.Phase.ShouldBe(CrashLikeGameComponent.Playing);
    }

    [Fact]
    public void OnUpdate_DepthZone_RunsForwardWithoutInput()
    {
        var world = CreateWorld();
        var zone = new Entity(10, "Zone");
        zone.AddComponent(new CameraZoneComponent
        {
            Mode = CrashLikeGameComponent.Depth,
            MinX = -10f,
            MaxX = 10f,
            LookAhead = 3f,
            OrthoSize = 4f
        });
        world.Context.Register(zone);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        world.Player.GetComponent<RigidBody2DComponent>().Velocity.X.ShouldBeGreaterThan(0.5f);
        world.Player.GetComponent<PlayerMotorComponent>().Facing.ShouldBe(1);
    }

    [Fact]
    public void OnUpdate_Fall_LosesALifeAndArmsRespawn()
    {
        var world = CreateWorld();
        world.Game.DeathY = -2f;
        world.Game.RespawnX = 4f;
        world.Game.RespawnY = 2f;
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(0f, -5f, 0f);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Lives.ShouldBe(2);
        world.Game.TeleportPending.ShouldBeTrue();
        world.Player.GetComponent<TransformComponent>().Translation.Y.ShouldBe(2f);
        world.Player.GetComponent<RigidBody2DComponent>().BodyType.ShouldBe(RigidBodyType.Kinematic);
    }

    [Fact]
    public void OnUpdate_Pit_DoesNotSpendTheMask()
    {
        var world = CreateWorld();
        world.Game.Masks = 1;
        world.Game.DeathY = -1f;
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(0f, -4f, 0f);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Masks.ShouldBe(1);
        world.Game.Lives.ShouldBe(2);
    }

    [Fact]
    public void OnUpdate_Wumpa_IncrementsAndHides()
    {
        var world = CreateWorld();
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        var fruit = new Entity(20, "Wumpa");
        fruit.AddComponent(new TransformComponent { Translation = at });
        fruit.AddComponent(new SpriteRendererComponent());
        fruit.AddComponent(new CollectibleComponent
        {
            PickupType = CollectibleComponent.Wumpa,
            Value = 1,
            Radius = 1f,
            HomeX = at.X,
            HomeY = at.Y
        });
        world.Context.Register(fruit);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Wumpa.ShouldBe(1);
        fruit.GetComponent<CollectibleComponent>().Taken.ShouldBeTrue();
        fruit.GetComponent<SpriteRendererComponent>().Color.W.ShouldBe(0f);
    }

    [Fact]
    public void OnUpdate_Spin_BreaksOverlappingCrate()
    {
        var world = CreateWorld();
        world.Game.SpinTimer = 0.5f;
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        var crate = new Entity(21, "Crate");
        crate.AddComponent(new TransformComponent { Translation = at });
        crate.AddComponent(new SpriteRendererComponent());
        crate.AddComponent(new BoxCollider2DComponent());
        crate.AddComponent(new CrateComponent
        {
            CrateType = CrateComponent.Basic,
            LootWumpa = 1,
            HomeX = at.X,
            HomeY = at.Y,
            HalfWidth = 0.5f,
            HalfHeight = 0.5f
        });
        world.Context.Register(crate);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        crate.GetComponent<CrateComponent>().Broken.ShouldBeTrue();
        world.Game.Wumpa.ShouldBe(1);
    }

    [Fact]
    public void OnUpdate_StackedCrate_DropsWhenSupportBreaks()
    {
        var world = CreateWorld();
        world.Game.SpinTimer = 0.5f;
        var motor = world.Player.GetComponent<PlayerMotorComponent>();
        motor.HalfWidth = 0.05f;
        motor.HalfHeight = 0.05f;
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(2f, 0.5f, 0f);
        var bottom = Crate(21, 2f, 0.5f, loot: 0);
        var top = Crate(22, 2f, 1.5f, loot: 0);
        world.Context.Register(bottom);
        world.Context.Register(top);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        bottom.GetComponent<CrateComponent>().Broken.ShouldBeTrue();
        top.GetComponent<CrateComponent>().Broken.ShouldBeFalse();
        top.GetComponent<TransformComponent>().Translation.Y.ShouldBe(0.5f, 0.02f);
    }

    [Fact]
    public void OnUpdate_Hazard_SpendsMaskInsteadOfALife()
    {
        var world = CreateWorld();
        world.Game.Masks = 1;
        var hazard = new Entity(22, "Spikes");
        hazard.AddComponent(new TransformComponent
        {
            Translation = world.Player.GetComponent<TransformComponent>().Translation
        });
        hazard.AddComponent(new HazardComponent { HalfWidth = 1f, HalfHeight = 1f });
        world.Context.Register(hazard);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Masks.ShouldBe(0);
        world.Game.Lives.ShouldBe(3);
        world.Game.TeleportPending.ShouldBeFalse();
    }

    [Fact]
    public void OnUpdate_RAfterGameOver_RestoresTheRun()
    {
        var world = CreateWorld();
        world.Game.Phase = CrashLikeGameComponent.GameOver;
        world.Game.Lives = 0;
        world.Game.Wumpa = 7;
        world.Game.StartingLives = 3;
        world.Keyboard.WasKeyPressed(KeyCodes.R).Returns(true);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Phase.ShouldBe(CrashLikeGameComponent.Playing);
        world.Game.Lives.ShouldBe(3);
        world.Game.Wumpa.ShouldBe(0);
        world.Game.TeleportPending.ShouldBeTrue();
        world.Player.GetComponent<TransformComponent>().Translation.X.ShouldBe(world.Game.StartX);
    }

    private static Entity Crate(int id, float x, float y, int loot)
    {
        var crate = new Entity(id, $"Crate{id}");
        crate.AddComponent(new TransformComponent { Translation = new Vector3(x, y, 0f) });
        crate.AddComponent(new SpriteRendererComponent());
        crate.AddComponent(new BoxCollider2DComponent());
        crate.AddComponent(new CrateComponent
        {
            CrateType = CrateComponent.Basic,
            LootWumpa = loot,
            HomeX = x,
            HomeY = y,
            HalfWidth = 0.5f,
            HalfHeight = 0.5f
        });
        return crate;
    }

    private static World CreateWorld()
    {
        var context = new Context();
        var gameEntity = new Entity(1, "Game");
        var game = new CrashLikeGameComponent
        {
            Lives = 3,
            StartingLives = 3,
            MaxLives = 5,
            StartX = 2f,
            StartY = 1f,
            RespawnX = 2f,
            RespawnY = 1f,
            MoveSpeed = 6.5f,
            GroundAccel = 70f,
            AirAccel = 28f,
            JumpSpeed = 10f,
            DeathY = -3f
        };
        gameEntity.AddComponent(game);
        context.Register(gameEntity);

        var player = new Entity(2, "Player");
        player.AddComponent(new TransformComponent { Translation = new Vector3(2f, 1f, 0f) });
        player.AddComponent(new RigidBody2DComponent { BodyType = RigidBodyType.Dynamic, FixedRotation = true });
        player.AddComponent(new SpriteRendererComponent());
        player.AddComponent(new PlayerMotorComponent { HalfWidth = 0.3f, HalfHeight = 0.5f, Facing = 1 });
        context.Register(player);

        var camera = new Entity(3, "Camera");
        camera.AddComponent(new TransformComponent { Translation = new Vector3(8f, 2.2f, 0f) });
        camera.AddComponent(new CameraComponent { OrthographicSize = 4.6f, AspectRatio = 16f / 9f, Primary = true });
        context.Register(camera);

        var keyboard = Substitute.For<IKeyboardInput>();
        var system = new CrashLikeSystem(context, keyboard, Substitute.For<IPhysicsQueries>(), Substitute.For<IAudio>());
        return new World(context, system, game, player, keyboard);
    }

    private sealed record World(
        Context Context,
        CrashLikeSystem System,
        CrashLikeGameComponent Game,
        Entity Player,
        IKeyboardInput Keyboard);
}
