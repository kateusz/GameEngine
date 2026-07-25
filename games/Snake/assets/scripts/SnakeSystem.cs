using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents.Rendering;
using Scripting;

namespace Snake.project.assets.scripts;

[Register(typeof(IGameSystem))]
public class SnakeSystem(IContext context, IKeyboardInput keyboardInput, IAudio audio) : IGameSystem
{
    private const string TexApple = "textures/snake/apple.png";
    private const string TexCell = "textures/cell.png";
    private static readonly Vector4 EmptyColor = new(0.08f, 0.12f, 0.08f, 1f);

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
            if (game.GameOver)
            {
                audio.PlayOneShot("assets/sounds/gameover.wav");
                break;
            }
            if (game.Score > scoreBefore)
                audio.PlayOneShot("assets/sounds/eat.wav");
            else
                audio.PlayOneShot("assets/sounds/move.wav");
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
        var segmentTextures = BuildSegmentTextures(game);

        foreach (var (entity, cell) in context.View<GridCellComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            var index = cell.Index;
            if (index == game.FoodIndex)
            {
                sprite.TexturePath = TexApple;
                sprite.Color = Vector4.One;
            }
            else if (segmentTextures.TryGetValue(index, out var path))
            {
                sprite.TexturePath = path;
                sprite.Color = Vector4.One;
            }
            else
            {
                sprite.TexturePath = TexCell;
                sprite.Color = EmptyColor;
            }
        }
    }

    // ponytail: O(n) dict per frame — fine for 16x12; upgrade to dirty-flag sync if grid grows a lot
    public static Dictionary<int, string> BuildSegmentTextures(SnakeGameComponent game)
    {
        var body = game.Body;
        var map = new Dictionary<int, string>(body.Length);
        if (body.Length == 0)
            return map;

        map[body[0]] = HeadTexture(game.Direction);
        if (body.Length == 1)
            return map;

        map[body[^1]] = TailTexture(game, body[^1], body[^2]);
        for (var i = 1; i < body.Length - 1; i++)
            map[body[i]] = MidTexture(game, body[i - 1], body[i], body[i + 1]);

        return map;
    }

    public static string HeadTexture(int direction) => direction switch
    {
        SnakeGameComponent.Up => "textures/snake/head_up.png",
        SnakeGameComponent.Down => "textures/snake/head_down.png",
        SnakeGameComponent.Left => "textures/snake/head_left.png",
        _ => "textures/snake/head_right.png"
    };

    public static string TailTexture(SnakeGameComponent game, int tip, int towardHead)
    {
        var (dx, dy) = Offset(game, tip, towardHead);
        // Tip sprite points away from body; flat edge faces towardHead.
        if (dy > 0) return "textures/snake/tail_up.png";
        if (dy < 0) return "textures/snake/tail_down.png";
        if (dx > 0) return "textures/snake/tail_left.png";
        return "textures/snake/tail_right.png";
    }

    public static string MidTexture(SnakeGameComponent game, int prev, int curr, int next)
    {
        var (dx1, dy1) = Offset(game, curr, prev);
        var (dx2, dy2) = Offset(game, curr, next);
        var up = dy1 < 0 || dy2 < 0;
        var down = dy1 > 0 || dy2 > 0;
        var left = dx1 < 0 || dx2 < 0;
        var right = dx1 > 0 || dx2 > 0;

        if (up && down) return "textures/snake/body_vertical.png";
        if (left && right) return "textures/snake/body_horizontal.png";
        if (up && left) return "textures/snake/body_topleft.png";
        if (up && right) return "textures/snake/body_topright.png";
        if (down && left) return "textures/snake/body_bottomleft.png";
        if (down && right) return "textures/snake/body_bottomright.png";
        return "textures/snake/body_horizontal.png";
    }

    private static (int dx, int dy) Offset(SnakeGameComponent game, int from, int to)
    {
        var fx = from % game.GridWidth;
        var fy = from / game.GridWidth;
        var tx = to % game.GridWidth;
        var ty = to / game.GridWidth;
        return (tx - fx, ty - fy);
    }

    private void SyncBanners(SnakeGameComponent game)
    {
        SetBanner("GameOverBanner", game.GameOver ? "textures/gameover.png" : null); ;
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
