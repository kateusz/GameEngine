using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;

namespace FlappyBird.project.assets.scripts;

[Register(typeof(IGameSystem))]
public class FlappyBirdSystem(IContext context, IKeyboardInput keyboardInput, IAudio audio) : IGameSystem
{
    private const float ReadyBirdY = 0.3f;
    private const float BirdHalfWidth = 0.13f;
    private const float BirdHalfHeight = 0.10f;
    private const float PipeHalfWidth = 0.24f;
    private const float PipeVisualHalf = 1.6f;
    private const float GroundTileWidth = 3.36f;
    private const float DigitSpacing = 0.34f;
    private const float DigitY = 2.0f;
    private const float LeftDespawnEdge = -4.8f;
    private const float GapMargin = 0.25f;

    private const string BirdFrameUp = "textures/Game Objects/yellowbird-upflap.png";
    private const string BirdFrameMid = "textures/Game Objects/yellowbird-midflap.png";
    private const string BirdFrameDown = "textures/Game Objects/yellowbird-downflap.png";

    private bool _initialized;
    private bool _diePlayed;
    private Entity[]? _groundTiles;

    public int Priority => 115;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        // Clamp dt so a lag spike (alt-tab, GC pause) can't tunnel the bird through a pipe.
        var dt = (float)deltaTime.TotalSeconds;
        if (dt > 0.05f)
            dt = 0.05f;

        if (!_initialized)
        {
            EnterReady(game);
            _initialized = true;
        }

        switch (game.Phase)
        {
            case FlappyBirdGameComponent.Ready:
                UpdateReady(game, dt);
                break;
            case FlappyBirdGameComponent.Playing:
                UpdatePlaying(game, dt);
                break;
            case FlappyBirdGameComponent.Dead:
                UpdateDead(game, dt);
                break;
        }

        SyncBird(game);
        SyncPipes(game);
        SyncGround(game);
        SyncScore(game);
        SyncBanners(game);
    }

    private FlappyBirdGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<FlappyBirdGameComponent>())
            return game;
        return null;
    }

    private void EnterReady(FlappyBirdGameComponent game)
    {
        game.Phase = FlappyBirdGameComponent.Ready;
        game.BirdY = ReadyBirdY;
        game.BirdVelocity = 0f;
        game.BobT = 0f;
        game.FlapAnimT = 0f;
        game.GroundScroll = 0f;
        game.Score = 0;

        for (var i = 0; i < game.PipeX.Length; i++)
        {
            game.PipeX[i] = game.FirstPipeX + i * game.PipeSpacing;
            game.PipeGapY[i] = RandomGapY(game);
            game.PipeScored[i] = false;
        }

        _diePlayed = false;
        audio.PlayOneShot("assets/audio/swoosh.wav");
    }

    private void UpdateReady(FlappyBirdGameComponent game, float dt)
    {
        game.BobT += dt;
        game.BirdY = ReadyBirdY + MathF.Sin(game.BobT * 5f) * 0.12f;
        game.FlapAnimT += dt;
        game.GroundScroll += game.PipeSpeed * dt;

        if (FlapPressed())
        {
            game.Phase = FlappyBirdGameComponent.Playing;
            game.BirdVelocity = game.FlapVelocity;
            game.BobT = 0f;
            audio.PlayOneShot("assets/audio/wing.wav");
        }
    }

    private void UpdatePlaying(FlappyBirdGameComponent game, float dt)
    {
        if (FlapPressed())
        {
            game.BirdVelocity = game.FlapVelocity;
            audio.PlayOneShot("assets/audio/wing.wav");
        }

        game.BirdVelocity -= game.Gravity * dt;
        game.BirdY += game.BirdVelocity * dt;
        game.FlapAnimT += dt;
        game.GroundScroll += game.PipeSpeed * dt;

        var ceiling = game.CeilingY - BirdHalfHeight;
        if (game.BirdY > ceiling)
        {
            game.BirdY = ceiling;
            if (game.BirdVelocity > 0f)
                game.BirdVelocity = 0f;
        }

        AdvancePipes(game, dt);

        if (HitsGround(game) || HitsAnyPipe(game))
        {
            game.Phase = FlappyBirdGameComponent.Dead;
            _diePlayed = false;
            audio.PlayOneShot("assets/audio/hit.wav");
            return;
        }

        if (ScorePassedPipes(game) > 0)
            audio.PlayOneShot("assets/audio/point.wav");
    }

    private void UpdateDead(FlappyBirdGameComponent game, float dt)
    {
        game.BirdVelocity -= game.Gravity * dt;
        game.BirdY += game.BirdVelocity * dt;

        var rest = game.GroundTopY + BirdHalfHeight;
        if (game.BirdY <= rest)
        {
            game.BirdY = rest;
            game.BirdVelocity = 0f;
            if (!_diePlayed)
            {
                audio.PlayOneShot("assets/audio/die.wav");
                _diePlayed = true;
            }
        }

        if (RestartPressed())
            EnterReady(game);
    }

    // --- Pure logic (static so it is unit-testable without a running engine) ---

    public static void AdvancePipes(FlappyBirdGameComponent game, float dt)
    {
        for (var i = 0; i < game.PipeX.Length; i++)
        {
            game.PipeX[i] -= game.PipeSpeed * dt;
            if (game.PipeX[i] < LeftDespawnEdge)
            {
                game.PipeX[i] = MaxPipeX(game) + game.PipeSpacing;
                game.PipeGapY[i] = RandomGapY(game);
                game.PipeScored[i] = false;
            }
        }
    }

    public static int ScorePassedPipes(FlappyBirdGameComponent game)
    {
        var gained = 0;
        for (var i = 0; i < game.PipeX.Length; i++)
        {
            if (!game.PipeScored[i] && game.PipeX[i] < game.BirdX)
            {
                game.PipeScored[i] = true;
                game.Score++;
                gained++;
            }
        }

        return gained;
    }

    public static bool HitsGround(FlappyBirdGameComponent game) =>
        game.BirdY - BirdHalfHeight <= game.GroundTopY;

    public static bool HitsAnyPipe(FlappyBirdGameComponent game)
    {
        var halfGap = game.PipeGap * 0.5f;
        for (var i = 0; i < game.PipeX.Length; i++)
        {
            if (MathF.Abs(game.BirdX - game.PipeX[i]) > BirdHalfWidth + PipeHalfWidth)
                continue;

            var gapTop = game.PipeGapY[i] + halfGap;
            var gapBottom = game.PipeGapY[i] - halfGap;
            if (game.BirdY + BirdHalfHeight > gapTop || game.BirdY - BirdHalfHeight < gapBottom)
                return true;
        }

        return false;
    }

    public static float RandomGapY(FlappyBirdGameComponent game)
    {
        var halfGap = game.PipeGap * 0.5f;
        var min = game.GroundTopY + halfGap + GapMargin;
        var max = game.CeilingY - halfGap - GapMargin;
        return min + (float)Random.Shared.NextDouble() * (max - min);
    }

    private static float MaxPipeX(FlappyBirdGameComponent game)
    {
        var max = game.PipeX[0];
        for (var i = 1; i < game.PipeX.Length; i++)
        {
            if (game.PipeX[i] > max)
                max = game.PipeX[i];
        }

        return max;
    }

    // --- Input ---

    private bool FlapPressed() =>
        keyboardInput.WasKeyPressed(KeyCodes.Space) || keyboardInput.WasKeyPressed(KeyCodes.Up);

    private bool RestartPressed() =>
        keyboardInput.WasKeyPressed(KeyCodes.R) ||
        keyboardInput.WasKeyPressed(KeyCodes.Space) ||
        keyboardInput.WasKeyPressed(KeyCodes.Enter);

    // --- Visual sync ---

    private void SyncBird(FlappyBirdGameComponent game)
    {
        var bird = context.GetByName("Bird");
        if (bird == null)
            return;

        if (bird.TryGetComponent<TransformComponent>(out var transform))
        {
            var p = transform.Translation;
            transform.Translation = new Vector3(game.BirdX, game.BirdY, p.Z);

            var r = transform.Rotation;
            var tilt = game.Phase == FlappyBirdGameComponent.Ready
                ? 0f
                : System.Math.Clamp(game.BirdVelocity * 0.2f, -1.4f, 0.5f);
            transform.Rotation = new Vector3(r.X, r.Y, tilt);
        }

        if (bird.TryGetComponent<SpriteRendererComponent>(out var sprite))
            sprite.TexturePath = BirdFrame(game);
    }

    private static string BirdFrame(FlappyBirdGameComponent game)
    {
        if (game.Phase == FlappyBirdGameComponent.Dead)
            return BirdFrameDown;

        var frame = (int)(game.FlapAnimT * 9f) % 4;
        return frame switch
        {
            0 => BirdFrameUp,
            1 => BirdFrameMid,
            2 => BirdFrameDown,
            _ => BirdFrameMid
        };
    }

    private void SyncPipes(FlappyBirdGameComponent game)
    {
        var halfGap = game.PipeGap * 0.5f;
        foreach (var (entity, pipe) in context.View<PipePairComponent>())
        {
            if (pipe.Index < 0 || pipe.Index >= game.PipeX.Length)
                continue;
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            var gap = game.PipeGapY[pipe.Index];
            var y = pipe.IsTop
                ? gap + halfGap + PipeVisualHalf
                : gap - halfGap - PipeVisualHalf;

            var p = transform.Translation;
            transform.Translation = new Vector3(game.PipeX[pipe.Index], y, p.Z);
        }
    }

    private void SyncGround(FlappyBirdGameComponent game)
    {
        var tiles = GroundTiles();
        if (tiles.Length == 0)
            return;

        var total = tiles.Length * GroundTileWidth;
        var scroll = game.GroundScroll % total;
        for (var i = 0; i < tiles.Length; i++)
        {
            if (!tiles[i].TryGetComponent<TransformComponent>(out var transform))
                continue;

            var x = i * GroundTileWidth - scroll;
            x = ((x % total) + total) % total;
            if (x > total * 0.5f)
                x -= total;

            var p = transform.Translation;
            transform.Translation = new Vector3(x, p.Y, p.Z);
        }
    }

    private Entity[] GroundTiles()
    {
        if (_groundTiles != null)
            return _groundTiles;

        var tiles = new List<Entity>();
        for (var i = 0; ; i++)
        {
            var tile = context.GetByName($"Ground{i}");
            if (tile == null)
                break;
            tiles.Add(tile);
        }

        _groundTiles = tiles.ToArray();
        return _groundTiles;
    }

    private void SyncScore(FlappyBirdGameComponent game)
    {
        var text = game.Score.ToString();

        var capacity = 0;
        foreach (var _ in context.View<ScoreDigitComponent>())
            capacity++;
        if (capacity > 0 && text.Length > capacity)
            text = text[^capacity..];

        var digits = text.Length;
        foreach (var (entity, digit) in context.View<ScoreDigitComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            if (digit.Place < digits)
            {
                var x = (digit.Place - (digits - 1) * 0.5f) * DigitSpacing;
                var p = transform.Translation;
                transform.Translation = new Vector3(x, DigitY, p.Z);
                sprite.TexturePath = $"textures/UI/Numbers/{text[digit.Place]}.png";
                sprite.Color = Vector4.One;
            }
            else
            {
                sprite.TexturePath = null;
                sprite.Color = Vector4.Zero;
            }
        }
    }

    private void SyncBanners(FlappyBirdGameComponent game)
    {
        SetBanner("MessageBanner",
            game.Phase == FlappyBirdGameComponent.Ready ? "textures/UI/message.png" : null);
        SetBanner("GameOverBanner",
            game.Phase == FlappyBirdGameComponent.Dead ? "textures/UI/gameover.png" : null);
    }

    private void SetBanner(string entityName, string? texturePath)
    {
        var entity = context.GetByName(entityName);
        if (entity == null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        sprite.TexturePath = texturePath;
        sprite.Color = texturePath == null ? Vector4.Zero : Vector4.One;
    }
}
