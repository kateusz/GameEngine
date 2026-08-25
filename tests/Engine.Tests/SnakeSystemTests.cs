using Audio;
using ECS;
using Input;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;
using Snake.assets.scripts;

namespace Engine.Tests;

public class SnakeSystemTests
{
    [Fact]
    public void Step_IntoWall_SetsGameOver()
    {
        var game = CreateGame();
        game.Body = [0];
        game.Direction = SnakeGameComponent.Up;
        game.PendingDirection = SnakeGameComponent.Up;
        game.FoodIndex = 50;

        SnakeSystem.Step(game);

        game.GameOver.ShouldBeTrue();
    }

    [Fact]
    public void Step_IntoSelf_SetsGameOver()
    {
        var game = CreateGame();
        game.Body = [10, 11, 12, 13, 14, 15, 16, 9];
        game.Direction = SnakeGameComponent.Right;
        game.PendingDirection = SnakeGameComponent.Right;
        game.FoodIndex = 50;

        SnakeSystem.Step(game);

        game.GameOver.ShouldBeTrue();
    }

    [Fact]
    public void Step_EatingFood_GrowsSnakeAndIncrementsScore()
    {
        var game = CreateGame();
        game.Body = [100, 99, 98];
        game.Direction = SnakeGameComponent.Right;
        game.PendingDirection = SnakeGameComponent.Right;
        game.FoodIndex = 101;

        SnakeSystem.Step(game);

        game.Body.Length.ShouldBe(4);
        game.Body[0].ShouldBe(101);
        game.Score.ShouldBe(1);
        game.FoodIndex.ShouldNotBe(101);
    }

    [Fact]
    public void ResetGame_ClearsScoreAndBody()
    {
        var game = CreateGame();
        game.Body = [1, 2, 3, 4, 5];
        game.Score = 9;
        game.GameOver = true;
        game.Paused = true;

        SnakeSystem.ResetGame(game);

        game.GameOver.ShouldBeFalse();
        game.Paused.ShouldBeFalse();
        game.Score.ShouldBe(0);
        game.Body.Length.ShouldBe(3);
        game.FoodIndex.ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void OnUpdate_WhenPaused_DoesNotTickOrTurn()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        keyboard.WasKeyPressed(KeyCodes.Up).Returns(true);

        var (system, game) = CreateSystemWithGame(keyboard);
        var body = (int[])game.Body.Clone();
        game.Paused = true;
        game.TickAccumulator = 10;
        game.PendingDirection = SnakeGameComponent.Right;

        system.OnUpdate(TimeSpan.FromSeconds(1));

        game.Body.ShouldBe(body);
        game.PendingDirection.ShouldBe(SnakeGameComponent.Right);
        game.TickAccumulator.ShouldBe(10);
    }

    [Fact]
    public void OnUpdate_WasKeyPressedChangesDirection()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        keyboard.WasKeyPressed(KeyCodes.Up).Returns(true);

        var (system, game) = CreateSystemWithGame(keyboard);
        game.PendingDirection = SnakeGameComponent.Right;
        system.OnUpdate(TimeSpan.Zero);

        game.PendingDirection.ShouldBe(SnakeGameComponent.Up);
    }

    private static SnakeGameComponent CreateGame()
    {
        var game = new SnakeGameComponent();
        game.Reset();
        return game;
    }

    private static (SnakeSystem System, SnakeGameComponent Game) CreateSystemWithGame(IKeyboardInput? keyboard = null)
    {
        var context = new Context();
        var entity = Entity.Create(1, "game");
        var game = CreateGame();
        entity.AddComponent(game);
        context.Register(entity);

        var banner = Entity.Create(2, "GameOverBanner");
        banner.AddComponent(new SpriteRendererComponent());
        context.Register(banner);

        keyboard ??= Substitute.For<IKeyboardInput>();
        var audio = Substitute.For<IAudio>();
        return (new SnakeSystem(context, keyboard, audio), game);
    }
}
