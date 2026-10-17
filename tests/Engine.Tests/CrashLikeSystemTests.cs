using System.Numerics;
using Audio;
using CrashLike.assets.scripts;
using ECS;
using Input;
using NSubstitute;
using SceneComponents;
using SceneComponents.Camera;
using Scripting;
using Shouldly;

namespace Engine.Tests;

public class CrashLikeSystemTests
{
    [Fact]
    public void OnUpdate_RightKey_StrafesOnX()
    {
        var world = CreateWorld();
        world.Keyboard.IsKeyDown(KeyCodes.D).Returns(true);
        var startX = world.Player.GetComponent<TransformComponent>().Translation.X;

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        world.Player.GetComponent<TransformComponent>().Translation.X.ShouldBeGreaterThan(startX);
    }

    [Fact]
    public void OnUpdate_AutoRun_AdvancesAlongZ()
    {
        var world = CreateWorld();
        var startZ = world.Player.GetComponent<TransformComponent>().Translation.Z;

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        world.Player.GetComponent<TransformComponent>().Translation.Z.ShouldBeGreaterThan(startZ + 0.05f);
    }

    [Fact]
    public void OnUpdate_SideZone_SwingsTheCamera()
    {
        var world = CreateWorld();
        var zone = new Entity(10, "Zone");
        zone.AddComponent(new CameraZoneComponent
        {
            Mode = CrashLikeGameComponent.Side,
            MinZ = -10f,
            MaxZ = 10f
        });
        world.Context.Register(zone);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        world.Game.CameraMode.ShouldBe(CrashLikeGameComponent.Side);
        world.Camera.GetComponent<CameraComponent>().ProjectionType.ShouldBe(CameraProjectionTypeData.Perspective);
    }

    [Fact]
    public void OnUpdate_Fall_LosesALifeAndRespawns()
    {
        var world = CreateWorld();
        world.Game.DeathY = -2f;
        world.Game.RespawnX = 1f;
        world.Game.RespawnY = 3f;
        world.Game.RespawnZ = 4f;
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(0f, -5f, 0f);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Lives.ShouldBe(2);
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        at.X.ShouldBe(1f);
        at.Y.ShouldBe(3f);
        at.Z.ShouldBe(4f);
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
        fruit.AddComponent(new CollectibleComponent
        {
            PickupType = CollectibleComponent.Wumpa,
            Value = 1,
            Radius = 1f,
            HomeX = at.X,
            HomeY = at.Y,
            HomeZ = at.Z
        });
        world.Context.Register(fruit);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Wumpa.ShouldBe(1);
        fruit.GetComponent<CollectibleComponent>().Taken.ShouldBeTrue();
        fruit.GetComponent<TransformComponent>().Translation.Y.ShouldBe(-40f);
    }

    [Fact]
    public void OnUpdate_Spin_BreaksOverlappingCrate()
    {
        var world = CreateWorld();
        world.Game.SpinTimer = 0.5f;
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        var crate = Crate(21, at.X, at.Y, at.Z, loot: 1);
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
        motor.HalfDepth = 0.05f;
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(2f, 0.5f, 1f);
        var bottom = Crate(21, 2f, 0.5f, 1f, loot: 0);
        var top = Crate(22, 2f, 1.5f, 1f, loot: 0);
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
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        var hazard = new Entity(22, "Spikes");
        hazard.AddComponent(new TransformComponent { Translation = at });
        hazard.AddComponent(new HazardComponent { HalfWidth = 1f, HalfHeight = 1f, HalfDepth = 1f });
        world.Context.Register(hazard);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.016));

        world.Game.Masks.ShouldBe(0);
        world.Game.Lives.ShouldBe(3);
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
        var at = world.Player.GetComponent<TransformComponent>().Translation;
        at.X.ShouldBe(world.Game.StartX);
        at.Z.ShouldBe(world.Game.StartZ);
    }

    [Fact]
    public void OnUpdate_Ground_StopsTheFall()
    {
        var world = CreateWorld();
        world.Player.GetComponent<TransformComponent>().Translation = new Vector3(0f, 0.54f, 0f);
        var ground = new Entity(30, "Ground");
        ground.AddComponent(new TransformComponent { Translation = new Vector3(0f, -0.5f, 0f) });
        ground.AddComponent(new SolidComponent { HalfX = 4f, HalfY = 0.5f, HalfZ = 4f });
        world.Context.Register(ground);

        world.System.OnUpdate(TimeSpan.FromSeconds(0.05));

        var motor = world.Player.GetComponent<PlayerMotorComponent>();
        motor.Grounded.ShouldBeTrue();
        world.Player.GetComponent<TransformComponent>().Translation.Y.ShouldBe(0.5f, 0.02f);
    }

    private static Entity Crate(int id, float x, float y, float z, int loot)
    {
        var crate = new Entity(id, $"Crate{id}");
        crate.AddComponent(new TransformComponent { Translation = new Vector3(x, y, z) });
        crate.AddComponent(new SolidComponent { HalfX = 0.5f, HalfY = 0.5f, HalfZ = 0.5f });
        crate.AddComponent(new CrateComponent
        {
            CrateType = CrateComponent.Basic,
            LootWumpa = loot,
            HomeX = x,
            HomeY = y,
            HomeZ = z,
            HalfWidth = 0.5f,
            HalfHeight = 0.5f,
            HalfDepth = 0.5f
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
            StartX = 0f,
            StartY = 1.2f,
            StartZ = 2f,
            RespawnX = 0f,
            RespawnY = 1.2f,
            RespawnZ = 2f,
            MoveSpeed = 7f,
            StrafeSpeed = 5.5f,
            Gravity = 18f,
            GroundAccel = 70f,
            AirAccel = 28f,
            JumpSpeed = 9.6f,
            DeathY = -3f
        };
        gameEntity.AddComponent(game);
        context.Register(gameEntity);

        var player = new Entity(2, "Player");
        player.AddComponent(new TransformComponent { Translation = new Vector3(0f, 1.2f, 2f) });
        player.AddComponent(new PlayerMotorComponent
        {
            HalfWidth = 0.3f,
            HalfHeight = 0.5f,
            HalfDepth = 0.3f
        });
        context.Register(player);

        var camera = new Entity(3, "Camera");
        camera.AddComponent(new TransformComponent { Translation = new Vector3(0f, 4f, -4f) });
        camera.AddComponent(new CameraComponent
        {
            ProjectionType = CameraProjectionTypeData.Perspective,
            PerspectiveFOV = 1f,
            PerspectiveNear = 0.1f,
            PerspectiveFar = 100f,
            AspectRatio = 16f / 9f,
            Primary = true
        });
        context.Register(camera);

        var keyboard = Substitute.For<IKeyboardInput>();
        var system = new CrashLikeSystem(context, keyboard, Substitute.For<IAudio>());
        return new World(context, system, game, player, camera, keyboard);
    }

    private sealed record World(
        Context Context,
        CrashLikeSystem System,
        CrashLikeGameComponent Game,
        Entity Player,
        Entity Camera,
        IKeyboardInput Keyboard);
}
