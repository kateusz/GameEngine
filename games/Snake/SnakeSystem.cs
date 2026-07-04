using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents.Rendering;
using Scripting;

namespace Snake;

[Register(typeof(IGameSystem))]
public class SnakeSystem(IContext context, IKeyboardInput keyboardInput, IAudio audio) : IGameSystem
{
    private static readonly Vector4 EmptyColor = new(0.08f, 0.12f, 0.08f, 1f);
    private static readonly Vector4 HeadColor = new(0.2f, 0.95f, 0.25f, 1f);
    private static readonly Vector4 BodyColor = new(0.1f, 0.65f, 0.15f, 1f);
    private static readonly Vector4 FoodColor = Vector4.One;

    public int Priority => 115;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        if (game.FoodIndex < 0 && !game.GameOver && game.Body.Length > 0)
            SpawnFood(game);

        HandleInput(game);

        if (game.GameOver)
        {
            SyncCellVisuals(game);
            SyncBanners(game);
            return;
        }

        game.TickAccumulator += deltaTime.TotalSeconds;
        while (game.TickAccumulator >= game.TickInterval)
        {
            game.TickAccumulator -= game.TickInterval;
            var scoreBefore = game.Score;
            Step(game);
            if (game.Score > scoreBefore)
                audio.PlayOneShot("assets/sounds/eat.wav");
            if (game.GameOver)
                break;
        }

        SyncCellVisuals(game);
        SyncBanners(game);
    }

    public void OnShutdown() { }

    private SnakeGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<SnakeGameComponent>())
            return game;
        return null;
    }

    private void HandleInput(SnakeGameComponent game)
    {
        if (keyboardInput.WasKeyPressed(KeyCodes.R))
        {
            ResetGame(game);
            return;
        }

        if (game.GameOver)
            return;

        var next = game.PendingDirection;
        if (keyboardInput.WasKeyPressed(KeyCodes.W) || keyboardInput.WasKeyPressed(KeyCodes.Up))
            next = SnakeGameComponent.Up;
        else if (keyboardInput.WasKeyPressed(KeyCodes.S) || keyboardInput.WasKeyPressed(KeyCodes.Down))
            next = SnakeGameComponent.Down;
        else if (keyboardInput.WasKeyPressed(KeyCodes.A) || keyboardInput.WasKeyPressed(KeyCodes.Left))
            next = SnakeGameComponent.Left;
        else if (keyboardInput.WasKeyPressed(KeyCodes.D) || keyboardInput.WasKeyPressed(KeyCodes.Right))
            next = SnakeGameComponent.Right;

        if (!IsOpposite(game.Direction, next))
            game.PendingDirection = next;
    }

    public static void ResetGame(SnakeGameComponent game)
    {
        game.Reset();
        SpawnFood(game);
        Console.WriteLine("Snake reset. Score: 0");
    }

    public static void Step(SnakeGameComponent game)
    {
        if (game.GameOver || game.Body.Length == 0)
            return;

        if (game.FoodIndex < 0)
            SpawnFood(game);

        game.Direction = game.PendingDirection;
        var head = game.Body[0];
        if (!TryGetNextIndex(game, head, game.Direction, out var next))
        {
            game.GameOver = true;
            Console.WriteLine($"Game over! Score: {game.Score}");
            return;
        }

        if (IsBodyCollision(game, next))
        {
            game.GameOver = true;
            Console.WriteLine($"Game over! Score: {game.Score}");
            return;
        }

        var ate = next == game.FoodIndex;
        if (ate)
        {
            var grown = new int[game.Body.Length + 1];
            grown[0] = next;
            Array.Copy(game.Body, 0, grown, 1, game.Body.Length);
            game.Body = grown;
            game.Score++;
            Console.WriteLine($"Score: {game.Score}");
            SpawnFood(game);
        }
        else
        {
            var moved = new int[game.Body.Length];
            moved[0] = next;
            Array.Copy(game.Body, 0, moved, 1, game.Body.Length - 1);
            game.Body = moved;
        }
    }

    public static void SpawnFood(SnakeGameComponent game)
    {
        var occupied = new HashSet<int>(game.Body);
        var free = new List<int>(game.CellCount - occupied.Count);
        for (var i = 0; i < game.CellCount; i++)
        {
            if (!occupied.Contains(i))
                free.Add(i);
        }

        if (free.Count == 0)
        {
            game.GameOver = true;
            game.FoodIndex = -1;
            Console.WriteLine("You win! Board full.");
            return;
        }

        game.FoodIndex = free[Random.Shared.Next(free.Count)];
    }

    private void SyncCellVisuals(SnakeGameComponent game)
    {
        var head = game.Body.Length > 0 ? game.Body[0] : -1;
        var body = game.Body.Length > 1
            ? new HashSet<int>(game.Body.AsSpan(1).ToArray())
            : [];

        foreach (var (entity, cell) in context.View<GridCellComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            var index = cell.Index;
            if (index == game.FoodIndex)
            {
                sprite.TexturePath = "textures/food.png";
                sprite.Color = FoodColor;
            }
            else if (index == head)
            {
                sprite.TexturePath = "textures/snake_head.png";
                sprite.Color = HeadColor;
            }
            else if (body.Contains(index))
            {
                sprite.TexturePath = "textures/snake_body.png";
                sprite.Color = BodyColor;
            }
            else
            {
                sprite.TexturePath = "textures/cell.png";
                sprite.Color = EmptyColor;
            }
        }
    }

    private void SyncBanners(SnakeGameComponent game)
    {
        SetBanner("GameOverBanner", game.GameOver ? "textures/game_over.png" : null);
        SetBanner("ResetHint", game.GameOver ? "textures/press_r.png" : null);
    }

    private void SetBanner(string entityName, string? texturePath)
    {
        var entity = context.GetByName(entityName);
        if (entity == null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        sprite.TexturePath = texturePath;
        sprite.Color = texturePath == null ? Vector4.Zero : Vector4.One;
    }

    private static bool TryGetNextIndex(SnakeGameComponent game, int index, int direction, out int next)
    {
        var x = index % game.GridWidth;
        var y = index / game.GridWidth;

        switch (direction)
        {
            case SnakeGameComponent.Up when y == 0:
            case SnakeGameComponent.Down when y == game.GridHeight - 1:
            case SnakeGameComponent.Left when x == 0:
            case SnakeGameComponent.Right when x == game.GridWidth - 1:
                next = -1;
                return false;
        }

        next = direction switch
        {
            SnakeGameComponent.Up => (y - 1) * game.GridWidth + x,
            SnakeGameComponent.Down => (y + 1) * game.GridWidth + x,
            SnakeGameComponent.Left => y * game.GridWidth + (x - 1),
            _ => y * game.GridWidth + (x + 1)
        };
        return true;
    }

    private static bool IsBodyCollision(SnakeGameComponent game, int index) =>
        game.Body.Contains(index);

    private static bool IsOpposite(int current, int next) =>
        current switch
        {
            SnakeGameComponent.Up => next == SnakeGameComponent.Down,
            SnakeGameComponent.Down => next == SnakeGameComponent.Up,
            SnakeGameComponent.Left => next == SnakeGameComponent.Right,
            SnakeGameComponent.Right => next == SnakeGameComponent.Left,
            _ => false
        };
}
