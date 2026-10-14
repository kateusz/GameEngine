using System.Numerics;
using Audio;
using ECS;
using ECS.Systems;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Scripting;

namespace Platformer.assets.scripts;

/// <summary>
/// Side-view platformer. Arrows or A/D move, Space or W jumps, R restarts.
/// Visuals are a recycled tile pool plus pooled enemies — the level itself lives on
/// <see cref="PlatformerGameComponent"/>.
/// </summary>
[Register(typeof(IGameSystem))]
public class PlatformerSystem(Context context, IKeyboardInput keyboard, IAudio audio) : IGameSystem
{
    private const string TexGroundLeft = "textures/platformer/ground-left.png";
    private const string TexGroundMid = "textures/platformer/ground-mid.png";
    private const string TexGroundRight = "textures/platformer/ground-right.png";
    private const string TexGroundFill = "textures/platformer/ground-fill.png";
    private const string TexGroundBottom = "textures/platformer/ground-bottom.png";
    private const string TexBrick = "textures/platformer/brick.png";
    private const string TexQuestion = "textures/platformer/question.png";
    private const string TexUsed = "textures/platformer/used.png";
    private const string TexWood = "textures/platformer/wood.png";
    private const string TexCoin = "textures/platformer/coin.png";
    private const string TexFlag = "textures/platformer/flag.png";
    private const string TexSpike = "textures/platformer/spike.png";
    private const string TexHeart = "textures/platformer/heart.png";
    private const string TexPlayer = "textures/platformer/player.png";
    private const string TexPlayerLeft = "textures/platformer/player-left.png";
    private const string TexEnemy = "textures/platformer/enemy.png";
    private const string TexReady = "textures/ui/ready.png";
    private const string TexOver = "textures/ui/gameover.png";
    private const string TexWin = "textures/ui/win.png";

    private const float DigitSpacing = 0.48f;

    public int Priority => 115;

    public void OnInit()
    {
        var game = FindGame();
        if (game == null)
            return;

        PlatformerSimulation.Reset(game);
        SyncAll(game);
    }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = FindGame();
        if (game == null)
            return;

        if (keyboard.WasKeyPressed(KeyCodes.R))
        {
            PlatformerSimulation.Reset(game);
            SyncAll(game);
            return;
        }

        ApplyCameraAspect(game);

        var dt = (float)deltaTime.TotalSeconds;
        var events = PlatformerSimulation.Tick(game, ReadInput(), dt);
        PlayEvents(events);
        SyncAll(game);
    }

    private PlatformerGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<PlatformerGameComponent>())
            return game;
        return null;
    }

    private void ApplyCameraAspect(PlatformerGameComponent game)
    {
        var camera = context.GetByName("Camera");
        if (camera.TryGetComponent<CameraComponent>(out var component) && component.AspectRatio > 0.2f)
            game.Aspect = component.AspectRatio;
    }

    private PlatformerInput ReadInput() => new()
    {
        Left = keyboard.IsKeyDown(KeyCodes.A) || keyboard.IsKeyDown(KeyCodes.Left),
        Right = keyboard.IsKeyDown(KeyCodes.D) || keyboard.IsKeyDown(KeyCodes.Right),
        JumpPressed = keyboard.WasKeyPressed(KeyCodes.Space) ||
                      keyboard.WasKeyPressed(KeyCodes.W) ||
                      keyboard.WasKeyPressed(KeyCodes.Up),
        JumpHeld = keyboard.IsKeyDown(KeyCodes.Space) ||
                   keyboard.IsKeyDown(KeyCodes.W) ||
                   keyboard.IsKeyDown(KeyCodes.Up)
    };

    private void PlayEvents(PlatformerEvents events)
    {
        if (events.Jumped)
            audio.PlayOneShot("sounds/jump.ogg");
        if (events.Coin)
            audio.PlayOneShot("sounds/coin.ogg");
        if (events.Stomped)
            audio.PlayOneShot("sounds/stomp.ogg");
        if (events.Hurt)
            audio.PlayOneShot("sounds/hurt.ogg");
        if (events.Won)
            audio.PlayOneShot("sounds/win.ogg");
    }

    private void SyncAll(PlatformerGameComponent game)
    {
        SyncCamera(game);
        SyncTiles(game);
        SyncPlayer(game);
        SyncEnemies(game);
        SyncClouds(game);
        SyncHud(game);
        SyncBanners(game);
    }

    private void SyncCamera(PlatformerGameComponent game)
    {
        var camera = context.GetByName("Camera");
        if (!camera.TryGetComponent<TransformComponent>(out var transform))
            return;

        var p = transform.Translation;
        transform.Translation = new Vector3(game.CamX, game.CamY, p.Z);
    }

    private void SyncTiles(PlatformerGameComponent game)
    {
        var size = game.TileSize;
        var viewLeft = game.CamX - game.OrthoSize * game.Aspect;
        var viewBottom = game.CamY - game.OrthoSize;
        var originCol = (int)MathF.Floor(viewLeft / size) - 1;
        var originRowBottom = (int)MathF.Floor(viewBottom / size) - 1;
        var columns = PlatformerGameComponent.PoolColumns;

        foreach (var (entity, cell) in context.View<LevelTileComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform) ||
                !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            var cx = cell.Index % columns;
            var cy = cell.Index / columns;
            if (cy < 0 || cy >= PlatformerGameComponent.PoolRows)
            {
                Hide(sprite, transform);
                continue;
            }

            var col = originCol + cx;
            var row = game.Height - 1 - (originRowBottom + cy);
            var ch = PlatformerSimulation.TileAt(game, col, row);
            var texture = TextureFor(game, col, row, ch);
            if (texture == null)
            {
                Hide(sprite, transform);
                continue;
            }

            PlatformerSimulation.TileCenter(game, col, row, out var x, out var y);
            var p = transform.Translation;
            transform.Translation = new Vector3(x, y, 0f);
            transform.Scale = new Vector3(size, size, 1f);
            sprite.TexturePath = texture;
            sprite.Color = Vector4.One;
        }
    }

    private static string? TextureFor(PlatformerGameComponent game, int col, int row, char ch) => ch switch
    {
        '#' => GroundTexture(game, col, row),
        'B' => TexBrick,
        '?' => TexQuestion,
        'U' => TexUsed,
        '=' => TexWood,
        'C' => TexCoin,
        'F' => TexFlag,
        '^' => TexSpike,
        _ => null
    };

    private static string GroundTexture(PlatformerGameComponent game, int col, int row)
    {
        var up = PlatformerSimulation.TileAt(game, col, row - 1) == '#';
        var down = PlatformerSimulation.TileAt(game, col, row + 1) == '#';
        var left = PlatformerSimulation.TileAt(game, col - 1, row) == '#';
        var right = PlatformerSimulation.TileAt(game, col + 1, row) == '#';
        if (!up)
        {
            if (!left && right)
                return TexGroundLeft;
            if (left && !right)
                return TexGroundRight;
            return TexGroundMid;
        }

        if (!down)
            return TexGroundBottom;
        return TexGroundFill;
    }

    private void SyncPlayer(PlatformerGameComponent game)
    {
        var player = context.GetByName("Player");
        if (!player.TryGetComponent<TransformComponent>(out var transform) ||
            !player.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        transform.Translation = new Vector3(game.PlayerX, game.PlayerY, 0.2f);
        transform.Scale = new Vector3(0.92f, 0.96f, 1f);
        sprite.TexturePath = game.Facing < 0 ? TexPlayerLeft : TexPlayer;
        var blink = game.Invuln > 0f && ((int)(game.Invuln * 14f) & 1) == 0;
        sprite.Color = blink ? new Vector4(1f, 1f, 1f, 0.35f) : Vector4.One;
    }

    private void SyncEnemies(PlatformerGameComponent game)
    {
        foreach (var (entity, slot) in context.View<EnemySlotComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform) ||
                !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            var i = slot.Index;
            if (i < 0 || i >= game.EnemyCount || !game.EnemyAlive[i])
            {
                Hide(sprite, transform);
                continue;
            }

            transform.Translation = new Vector3(game.EnemyX[i], game.EnemyY[i], 0.15f);
            transform.Scale = new Vector3(0.86f, 0.86f, 1f);
            sprite.TexturePath = TexEnemy;
            sprite.Color = Vector4.One;
        }
    }

    private void SyncClouds(PlatformerGameComponent game)
    {
        // Three named clouds drift slower than the camera so the sky has depth.
        SyncCloud("Cloud0", game, 0f, 4.2f, 3.6f);
        SyncCloud("Cloud1", game, 14f, 6.4f, 4.2f);
        SyncCloud("Cloud2", game, 28f, 5.1f, 3.2f);
    }

    private void SyncCloud(string name, PlatformerGameComponent game, float seed, float y, float width)
    {
        var cloud = context.GetByName(name);
        if (!cloud.TryGetComponent<TransformComponent>(out var transform))
            return;

        var span = MathF.Max(game.Width * game.TileSize, 1f);
        var x = seed + game.CamX * 0.35f;
        x = ((x % span) + span) % span;
        var p = transform.Translation;
        transform.Translation = new Vector3(x, y, -0.4f);
        transform.Scale = new Vector3(width, width * 0.65f, 1f);
    }

    private void SyncHud(PlatformerGameComponent game)
    {
        var halfW = game.OrthoSize * game.Aspect;
        var top = game.CamY + game.OrthoSize - 0.55f;

        var heart = context.GetByName("Heart");
        if (heart.TryGetComponent<TransformComponent>(out var heartTransform) &&
            heart.TryGetComponent<SpriteRendererComponent>(out var heartSprite))
        {
            heartTransform.Translation = new Vector3(game.CamX - halfW + 0.55f, top, 0.6f);
            heartTransform.Scale = new Vector3(0.48f, 0.48f, 1f);
            heartSprite.TexturePath = TexHeart;
            heartSprite.Color = Vector4.One;
        }

        var lives = context.GetByName("LivesDigit");
        if (lives.TryGetComponent<TransformComponent>(out var livesTransform) &&
            lives.TryGetComponent<SpriteRendererComponent>(out var livesSprite))
        {
            var digit = System.Math.Clamp(game.Lives, 0, 9);
            livesTransform.Translation = new Vector3(game.CamX - halfW + 1.1f, top, 0.6f);
            livesTransform.Scale = new Vector3(0.42f, 0.42f, 1f);
            livesSprite.TexturePath = $"textures/platformer/n{digit}.png";
            livesSprite.Color = Vector4.One;
        }

        var coin = context.GetByName("CoinIcon");
        if (coin.TryGetComponent<TransformComponent>(out var coinTransform) &&
            coin.TryGetComponent<SpriteRendererComponent>(out var coinSprite))
        {
            coinTransform.Translation = new Vector3(game.CamX - 1.7f, top, 0.6f);
            coinTransform.Scale = new Vector3(0.42f, 0.42f, 1f);
            coinSprite.TexturePath = TexCoin;
            coinSprite.Color = Vector4.One;
        }

        var text = game.Score.ToString();
        var shown = text.Length;
        foreach (var (entity, digit) in context.View<HudDigitComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform) ||
                !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            if (digit.Place < 0 || digit.Place >= shown)
            {
                Hide(sprite, transform);
                continue;
            }

            var x = game.CamX - 1.15f + digit.Place * DigitSpacing;
            transform.Translation = new Vector3(x, top, 0.6f);
            transform.Scale = new Vector3(0.42f, 0.42f, 1f);
            sprite.TexturePath = $"textures/platformer/n{text[digit.Place]}.png";
            sprite.Color = Vector4.One;
        }
    }

    private void SyncBanners(PlatformerGameComponent game)
    {
        var ready = game.Phase == PlatformerGameComponent.Ready ? TexReady : null;
        var over = game.Phase == PlatformerGameComponent.Dead ? TexOver : null;
        var win = game.Phase == PlatformerGameComponent.Won ? TexWin : null;
        SetBanner("ReadyBanner", ready, game, 0.4f, new Vector3(6.4f, 1.15f, 1f));
        SetBanner("GameOverBanner", over, game, 0.2f, new Vector3(5.6f, 1.3f, 1f));
        SetBanner("WinBanner", win, game, 0.2f, new Vector3(4.8f, 1.3f, 1f));
    }

    private void SetBanner(string name, string? texture, PlatformerGameComponent game, float yOffset, Vector3 scale)
    {
        var banner = context.GetByName(name);
        if (banner.TryGetComponent<SpriteRendererComponent>(out var sprite))
        {
            sprite.TexturePath = texture;
            sprite.Color = texture == null ? Vector4.Zero : Vector4.One;
        }

        if (!banner.TryGetComponent<TransformComponent>(out var transform))
            return;

        transform.Translation = new Vector3(game.CamX, game.CamY + yOffset, 0.8f);
        transform.Scale = texture == null ? Vector3.Zero : scale;
    }

    private static void Hide(SpriteRendererComponent sprite, TransformComponent transform)
    {
        sprite.TexturePath = null;
        sprite.Color = Vector4.Zero;
        transform.Scale = Vector3.Zero;
    }
}
