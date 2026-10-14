using ECS;
using ECS.Systems;
using Engine.Scene.Serializer;
using Engine.Scene.Systems;
using Platformer.assets.scripts;
using Shouldly;
using EngineScene = Engine.Scene.Scene;

namespace Engine.Tests;

public class PlatformerSimulationTests
{
    [Fact]
    public void DefaultLevel_IsRectangular_AndHasOnePlayerOneFlag()
    {
        var rows = PlatformerGameComponent.DefaultLevel();
        rows.Length.ShouldBeGreaterThan(4);
        var width = rows[0].Length;
        foreach (var row in rows)
            row.Length.ShouldBe(width);

        Count(rows, 'P').ShouldBe(1);
        Count(rows, 'F').ShouldBe(1);
        Count(rows, 'G').ShouldBeGreaterThan(0);
        Count(rows, '?').ShouldBeGreaterThan(0);
        Count(rows, 'C').ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Reset_SpawnsPlayerAndEnemies_AndClearsMarkers()
    {
        var game = NewGame();
        PlatformerSimulation.Reset(game);

        game.Phase.ShouldBe(PlatformerGameComponent.Ready);
        game.Lives.ShouldBe(3);
        game.EnemyCount.ShouldBe(2);
        game.EnemyAlive[0].ShouldBeTrue();
        game.EnemyAlive[1].ShouldBeTrue();
        Contains(game, 'P').ShouldBeFalse();
        Contains(game, 'G').ShouldBeFalse();
        PlatformerSimulation.SolidAt(game, game.PlayerX, game.PlayerY - game.PlayerHalfH - 0.2f).ShouldBeTrue();
    }

    [Fact]
    public void Tick_FallsOntoGround_AndStaysPut()
    {
        var game = Level(
            "....",
            "P...",
            "####");
        var y = game.PlayerY;

        for (var i = 0; i < 120; i++)
            PlatformerSimulation.Tick(game, default, Dt);

        game.Grounded.ShouldBeTrue();
        game.PlayerY.ShouldBeLessThan(y);
        var rested = game.PlayerY;
        for (var i = 0; i < 60; i++)
            PlatformerSimulation.Tick(game, default, Dt);
        game.PlayerY.ShouldBe(rested, 0.001f);
        game.VelY.ShouldBe(0f);
    }

    [Fact]
    public void Tick_Jump_LeavesTheGround()
    {
        var game = Level(
            "....",
            "P...",
            "####");
        Settle(game);
        var y = game.PlayerY;

        var events = PlatformerSimulation.Tick(game, new PlatformerInput { JumpPressed = true, JumpHeld = true }, Dt);
        events.Jumped.ShouldBeTrue();

        for (var i = 0; i < 8; i++)
            PlatformerSimulation.Tick(game, new PlatformerInput { JumpHeld = true }, Dt);

        game.PlayerY.ShouldBeGreaterThan(y + 0.4f);
        game.Grounded.ShouldBeFalse();
    }

    [Fact]
    public void Tick_WallBlocksHorizontalMove()
    {
        var game = Level(
            ".....",
            "P#...",
            "#####");
        Settle(game);
        var input = new PlatformerInput { Right = true };
        for (var i = 0; i < 90; i++)
            PlatformerSimulation.Tick(game, input, Dt);

        game.PlayerX.ShouldBeLessThan(1.5f);
        PlatformerSimulation.TileAt(game, 1, 1).ShouldBe('#');
    }

    [Fact]
    public void Tick_CollectsCoinOnce()
    {
        var game = Level(
            "....",
            "PC..",
            "####");
        Settle(game);
        var input = new PlatformerInput { Right = true };
        var gained = false;
        for (var i = 0; i < 90; i++)
        {
            var events = PlatformerSimulation.Tick(game, input, Dt);
            gained |= events.Coin;
        }

        gained.ShouldBeTrue();
        game.Coins.ShouldBe(1);
        game.Score.ShouldBe(game.CoinScore);
        Contains(game, 'C').ShouldBeFalse();

        var score = game.Score;
        PlatformerSimulation.Tick(game, input, Dt);
        game.Score.ShouldBe(score);
    }

    [Fact]
    public void Tick_HeadBumpTurnsQuestionBlockIntoUsedBlock()
    {
        var game = Level(
            "?....",
            ".....",
            ".....",
            "P....",
            "#####");
        Settle(game);

        var hold = new PlatformerInput { JumpPressed = true, JumpHeld = true };
        PlatformerSimulation.Tick(game, hold, Dt);
        for (var i = 0; i < 80; i++)
            PlatformerSimulation.Tick(game, new PlatformerInput { JumpHeld = true }, Dt);

        PlatformerSimulation.TileAt(game, 0, 0).ShouldBe('U');
        game.Coins.ShouldBe(1);
        game.Score.ShouldBe(game.CoinScore);
    }

    [Fact]
    public void Tick_SideTouchDoesNotBumpQuestionBlock()
    {
        var game = Level(
            "....",
            "?P..",
            "####");
        Settle(game);
        var input = new PlatformerInput { Left = true };
        for (var i = 0; i < 40; i++)
            PlatformerSimulation.Tick(game, input, Dt);

        PlatformerSimulation.TileAt(game, 0, 1).ShouldBe('?');
        game.Coins.ShouldBe(0);
    }

    [Fact]
    public void Tick_GoombaTurnsAtWall_AndStaysOnPlatform()
    {
        var game = Level(
            "........",
            "...G....",
            ".======.",
            "P.......",
            "########");
        var minX = 1f * game.TileSize;
        var maxX = 7f * game.TileSize;

        for (var i = 0; i < 240; i++)
            PlatformerSimulation.Tick(game, default, Dt);

        game.EnemyAlive[0].ShouldBeTrue();
        game.EnemyX[0].ShouldBeGreaterThan(minX);
        game.EnemyX[0].ShouldBeLessThan(maxX);
        game.EnemyVelX[0].ShouldNotBe(0f);
    }

    [Fact]
    public void Tick_StompKillsEnemyAndBounces()
    {
        var game = Level(
            "......",
            ".P.G..",
            "######");
        game.PlayerX = game.EnemyX[0];
        game.PlayerY = game.EnemyY[0] + 0.7f;
        game.VelY = -4f;

        var events = PlatformerSimulation.Tick(game, default, Dt);

        events.Stomped.ShouldBeTrue();
        game.EnemyAlive[0].ShouldBeFalse();
        game.Score.ShouldBe(game.StompScore);
        game.VelY.ShouldBe(game.StompBounce);
        game.Lives.ShouldBe(3);
    }

    [Fact]
    public void Tick_SideTouchCostsALifeAndRespawns()
    {
        var game = Level(
            "......",
            ".P.G..",
            "######");
        var spawnX = game.PlayerX;
        game.PlayerX = game.EnemyX[0] + 0.3f;
        game.PlayerY = game.EnemyY[0];
        game.VelY = 0f;
        game.Invuln = 0f;

        var events = PlatformerSimulation.Tick(game, default, Dt);

        events.Hurt.ShouldBeTrue();
        events.Stomped.ShouldBeFalse();
        game.Lives.ShouldBe(2);
        game.PlayerX.ShouldBe(spawnX, 0.001f);
        game.Phase.ShouldBe(PlatformerGameComponent.Playing);
    }

    [Fact]
    public void Tick_PitRemovesALife()
    {
        var game = Level(
            "P...",
            "#...");
        var input = new PlatformerInput { Right = true };
        for (var i = 0; i < 180 && game.Lives == 3; i++)
            PlatformerSimulation.Tick(game, input, Dt);

        game.Lives.ShouldBe(2);
        game.Phase.ShouldBe(PlatformerGameComponent.Playing);
    }

    [Fact]
    public void Tick_SpikesKillEvenDuringInvulnerability()
    {
        var game = Level(
            "P^..",
            "####");
        game.Invuln = 5f;
        var input = new PlatformerInput { Right = true };
        var hurt = false;
        for (var i = 0; i < 60 && game.Lives == 3; i++)
            hurt |= PlatformerSimulation.Tick(game, input, Dt).Hurt;

        hurt.ShouldBeTrue();
        game.Lives.ShouldBe(2);
    }

    [Fact]
    public void Tick_FlagWins()
    {
        var game = Level(
            "PF..",
            "####");
        var input = new PlatformerInput { Right = true };
        var won = false;
        for (var i = 0; i < 90 && game.Phase != PlatformerGameComponent.Won; i++)
            won |= PlatformerSimulation.Tick(game, input, Dt).Won;

        won.ShouldBeTrue();
        game.Phase.ShouldBe(PlatformerGameComponent.Won);
    }

    [Fact]
    public void Tick_OutOfLives_EndsTheRun()
    {
        var game = Level(
            "P^..",
            "####");
        game.Lives = 1;
        var input = new PlatformerInput { Right = true };
        PlatformerEvents last = default;
        for (var i = 0; i < 60 && game.Phase == PlatformerGameComponent.Playing; i++)
            last = PlatformerSimulation.Tick(game, input, Dt);

        last.Died.ShouldBeTrue();
        game.Phase.ShouldBe(PlatformerGameComponent.Dead);
        game.Lives.ShouldBe(0);
    }

    [Fact]
    public void Camera_StaysInsideAWideLevel()
    {
        var game = Level(
            "P...................................................................",
            "####################################################################");
        var halfW = game.OrthoSize * game.Aspect;

        game.CamX.ShouldBe(halfW, 0.01f);
        game.PlayerX.ShouldBeLessThan(halfW);

        game.PlayerX = game.Width * game.TileSize - 1f;
        PlatformerSimulation.UpdateCamera(game);
        game.CamX.ShouldBe(game.Width * game.TileSize - halfW, 0.01f);
    }

    [Fact]
    public void DefaultLevel_CanBeFinishedByRunningAndJumping()
    {
        var game = NewGame();
        PlatformerSimulation.Reset(game);
        game.Phase = PlatformerGameComponent.Playing;
        var hold = 0;

        for (var frame = 0; frame < 60 * 35 && game.Phase == PlatformerGameComponent.Playing; frame++)
        {
            var ahead = game.PlayerX + game.PlayerHalfW + 0.25f;
            var wall = PlatformerSimulation.SolidAt(game, ahead, game.PlayerY);
            var gap = game.Grounded &&
                      !PlatformerSimulation.SolidAt(game, game.PlayerX + 1.15f, game.PlayerY - game.PlayerHalfH - 0.25f);
            var startJump = game.Grounded && (wall || gap);
            if (startJump)
                hold = 18;

            PlatformerSimulation.Tick(game, new PlatformerInput
            {
                Right = true,
                JumpPressed = startJump,
                JumpHeld = hold > 0
            }, Dt);

            if (hold > 0)
                hold--;
        }

        game.Phase.ShouldBe(PlatformerGameComponent.Won);
        game.Lives.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Scene_LoadsPoolPlayerAndLevel()
    {
        var registry = new ComponentSerializerRegistry();
        registry.RegisterFromAssembly(typeof(PlatformerGameComponent).Assembly);
        var serializer = new SceneSerializer(registry, new SerializerOptions());
        using var scene = new EngineScene(
            "main",
            new Context(),
            new SystemManager(),
            new PhysicsRuntimeBodyStore(),
            new PhysicsContactQueue(),
            null!,
            NullCameraQueries.Instance);

        var skipped = serializer.Deserialize(scene, ScenePath());

        skipped.ShouldBeEmpty();
        scene.Dimension.ShouldBe(Engine.Scene.SceneDimension.TwoD);
        scene.Context.GetByName("Player").HasComponent<SceneComponents.TransformComponent>().ShouldBeTrue();
        scene.Context.GetByName("Camera").GetComponent<SceneComponents.Camera.CameraComponent>().Primary.ShouldBeTrue();
        var game = scene.Context.GetByName("Game").GetComponent<PlatformerGameComponent>();
        game.Width.ShouldBe(68);
        game.Height.ShouldBe(14);
        game.Lives.ShouldBe(3);

        var tiles = 0;
        foreach (var _ in scene.Context.View<LevelTileComponent>())
            tiles++;
        tiles.ShouldBe(PlatformerGameComponent.PoolColumns * PlatformerGameComponent.PoolRows);

        var enemies = 0;
        foreach (var _ in scene.Context.View<EnemySlotComponent>())
            enemies++;
        enemies.ShouldBe(PlatformerGameComponent.MaxEnemies);
    }

    private static string ScenePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "games", "Platformer", "assets", "scenes", "main.scene");
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException("games/Platformer/assets/scenes/main.scene");
    }

    private const float Dt = 1f / 60f;

    private static PlatformerGameComponent NewGame() => new();

    private static PlatformerGameComponent Level(params string[] rows)
    {
        var game = new PlatformerGameComponent { Template = rows };
        PlatformerSimulation.Reset(game);
        game.Phase = PlatformerGameComponent.Playing;
        return game;
    }

    private static void Settle(PlatformerGameComponent game)
    {
        for (var i = 0; i < 90; i++)
            PlatformerSimulation.Tick(game, default, Dt);
        game.Grounded.ShouldBeTrue();
    }

    private static int Count(string[] rows, char ch)
    {
        var n = 0;
        foreach (var row in rows)
            n += row.Count(c => c == ch);
        return n;
    }

    private static bool Contains(PlatformerGameComponent game, char ch)
    {
        foreach (var row in game.Rows)
        {
            if (row.Contains(ch))
                return true;
        }

        return false;
    }
}
