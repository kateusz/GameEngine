using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;

namespace Hounds.Marches;

[Register(typeof(IGameSystem))]
public sealed class CameraFollowSystem(Context context) : IGameSystem
{
    public int Priority => 112;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        var player = March.Player(context);
        var camera = March.Named(context, "Camera");
        if (game == null || player == null || camera == null)
            return;
        if (!player.TryGetComponent<TransformComponent>(out var playerTransform) ||
            !camera.TryGetComponent<TransformComponent>(out var cameraTransform))
            return;

        var dt = March.Dt(deltaTime);
        var room = March.Room(game);
        var targetX = System.Math.Clamp(playerTransform.Translation.X, room.CamMin, room.CamMax);
        var targetY = room.CamY + (playerTransform.Translation.Y - room.CamY) * 0.22f;
        var follow = 1f - MathF.Exp(-7f * dt);
        var cam = cameraTransform.Translation;
        cameraTransform.Translation = new Vector3(
            cam.X + (targetX - cam.X) * follow,
            cam.Y + (targetY - cam.Y) * follow,
            cam.Z);

        var camX = cameraTransform.Translation.X;
        foreach (var (entity, prop) in context.View<PropComponent>())
        {
            if (prop.Kind != "parallax" || !entity.TryGetComponent<TransformComponent>(out var transform))
                continue;
            var x = camX * (1f - prop.Factor) + prop.Anchor * prop.Factor;
            transform.Translation = new Vector3(x, prop.BaseY, 0f);
        }
    }
}

[Register(typeof(IGameSystem))]
public sealed class HudSystem(Context context) : IGameSystem
{
    private string _banner = "";

    public int Priority => 113;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var game = March.Game(context);
        var player = March.Player(context);
        if (game?.Pack == null || player == null)
            return;
        if (!player.TryGetComponent<HealthComponent>(out var health))
            return;
        player.TryGetComponent<StaminaComponent>(out var stamina);

        Bar("HpFill", -4.6f, 3.55f, 2.5f, 0.16f, health.Max <= 0 ? 0 : health.Current / health.Max);
        Bar("StamFill", -4.6f, 3.18f, 2.5f, 0.12f, stamina == null || stamina.Max <= 0 ? 0 : stamina.Current / stamina.Max);
        Digits(game, game.Stingers);

        var key = game.ProximityKey;
        if (key.Length == 0 && game.BannerTimer > 0f)
            key = game.BannerKey;
        if (key.Length == 0 && game.HelpTimer > 0f)
            key = "title_help";

        var banner = March.Named(context, "Banner");
        if (banner != null && banner.TryGetComponent<SpriteRendererComponent>(out var bannerSprite))
        {
            if (key.Length == 0)
            {
                bannerSprite.Color = new Vector4(0, 0, 0, 0);
            }
            else
            {
                var path = game.Pack.Image(key);
                if (path.Length > 0 && _banner != key)
                {
                    bannerSprite.TexturePath = path;
                    _banner = key;
                }

                bannerSprite.Color = path.Length == 0
                    ? new Vector4(0.35f, 0.12f, 0.1f, 0.85f)
                    : Vector4.One;
            }
        }

        var fade = March.Named(context, "Fade");
        if (fade != null && fade.TryGetComponent<SpriteRendererComponent>(out var fadeSprite))
        {
            var alpha = System.Math.Clamp(game.Fade, 0f, 1f);
            fadeSprite.Color = new Vector4(0.02f, 0.02f, 0.03f, alpha);
        }

        _ = deltaTime;
    }

    private void Bar(string name, float left, float y, float full, float h, float ratio)
    {
        var entity = March.Named(context, name);
        if (entity == null || !entity.TryGetComponent<TransformComponent>(out var transform))
            return;
        ratio = System.Math.Clamp(ratio, 0f, 1f);
        var width = MathF.Max(0.04f, full * ratio);
        transform.Scale = new Vector3(width, h, 1f);
        transform.Translation = new Vector3(left + width * 0.5f, y, 0f);
    }

    private void Digits(MarchGameComponent game, int value)
    {
        value = System.Math.Clamp(value, 0, 999);
        int[] digits = [value / 100, (value / 10) % 10, value % 10];
        var show = value >= 100 ? 3 : value >= 10 ? 2 : 1;
        for (var i = 0; i < 3; i++)
        {
            var entity = March.Named(context, "Digit" + i);
            if (entity == null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;
            var visible = i >= 3 - show;
            if (!visible)
            {
                sprite.Color = Vector4.Zero;
                continue;
            }

            var path = game.Pack!.Image("digit" + digits[i]);
            sprite.Color = path.Length == 0 ? new Vector4(0.9f, 0.8f, 0.55f, 1f) : Vector4.One;
            if (path.Length > 0 && sprite.TexturePath != path)
                sprite.TexturePath = path;
        }
    }
}
