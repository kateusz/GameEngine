using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Rendering;
using Scripting;

namespace IslandDash;

/// <summary>Pooled HUD entities, on the camera entity (runtime only).</summary>
public sealed class HudComponent : IGameComponent
{
    public List<int> PixelIds { get; set; } = [];
    public int UsedLastFrame { get; set; }
    public int CrystalIconId { get; set; } = -1;
    public int LivesIconId { get; set; } = -1;
    public float Time { get; set; }
    public IComponent Clone() => (HudComponent)MemberwiseClone();
}

/// <summary>
/// 3D HUD: the engine has no runtime UI (ImGui is editor-only) and sprites draw before 3D geometry, so the HUD is
/// built from emissive dot-matrix cubes and spinning model icons placed just in front of the camera every frame.
/// </summary>
[Register(typeof(IGameSystem))]
public class HudSystem(Context context) : IGameSystem
{
    /// <summary>Distance in front of the camera; must stay above the camera's near plane.</summary>
    private const float HudDistance = 0.3f;
    private const float PixelFill = 0.86f;
    private const float PixelDepth = 0.4f;
    private const float EmissiveStrength = 1.6f;
    private const float IconSize = 0.16f;
    private const float IconSpinRate = 2f;
    private const float BlinkRate = 3f;

    private const float CounterTextSize = 0.024f;
    private const float FlashTextSize = 0.03f;
    private const float TitleTextSize = 0.05f;
    private const float HeadlineTextSize = 0.042f;
    private const float PromptTextSize = 0.018f;
    private const float SmallTextSize = 0.013f;

    private static readonly Vector3 CrystalColor = new(0.35f, 0.75f, 1f);
    private static readonly Vector3 LivesColor = new(1f, 0.62f, 0.15f);
    private static readonly Vector3 FlashColor = new(1f, 0.92f, 0.3f);
    private static readonly Vector3 TitleColor = new(1f, 0.55f, 0.1f);
    private static readonly Vector3 PromptColor = new(1f, 1f, 1f);
    private static readonly Vector3 GameOverColor = new(1f, 0.2f, 0.15f);
    private static readonly Vector3 CompleteColor = new(0.4f, 1f, 0.45f);
    private static readonly Vector4 PixelAlbedo = new(0.02f, 0.02f, 0.02f, 1f);

    private enum Align { Left, Center, Right }

    public int Priority => SystemOrder.Hud;

    public void OnInit() { }

    public void OnShutdown() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (World.State(context) is not { } state || World.PrimaryCamera(context) is not { } cameraEntity ||
            !cameraEntity.TryGetComponent<CameraRigComponent>(out var rig))
            return;

        if (!cameraEntity.TryGetComponent<HudComponent>(out var hud))
            hud = cameraEntity.AddComponent(new HudComponent());
        hud.Time += (float)deltaTime.TotalSeconds;

        var camera = cameraEntity.GetComponent<CameraComponent>();
        var frame = new HudFrame(
            cameraEntity.GetComponent<TransformComponent>().Translation,
            rig.Orientation,
            HudDistance * MathF.Tan(camera.PerspectiveFOV * 0.5f),
            camera.AspectRatio);

        var used = 0;
        var showCounters = state.Phase is GamePhase.Playing or GamePhase.Dying or GamePhase.Paused;
        if (showCounters)
        {
            DrawText(hud, ref used, frame, $"x{state.Crystals}", -frame.Aspect + 0.24f, 0.86f, CounterTextSize, CrystalColor, Align.Left);
            DrawText(hud, ref used, frame, $"x{state.Lives}", frame.Aspect - 0.24f, 0.86f, CounterTextSize, LivesColor, Align.Right);
        }
        DrawIcons(hud, frame, showCounters);

        var blinkOn = MathF.Sin(hud.Time * BlinkRate * MathF.PI) > -0.3f;
        switch (state.Phase)
        {
            case GamePhase.Title:
                DrawText(hud, ref used, frame, "ISLAND DASH", 0f, 0.42f, TitleTextSize, TitleColor);
                if (blinkOn)
                    DrawText(hud, ref used, frame, "PRESS ENTER", 0f, -0.05f, PromptTextSize, PromptColor);
                DrawText(hud, ref used, frame, "WASD MOVE  SPACE JUMP  K SPIN", 0f, -0.6f, SmallTextSize, PromptColor);
                DrawText(hud, ref used, frame, "Q E CAMERA  P PAUSE", 0f, -0.72f, SmallTextSize, PromptColor);
                break;
            case GamePhase.Paused:
                DrawText(hud, ref used, frame, "PAUSED", 0f, 0.2f, HeadlineTextSize, PromptColor);
                DrawText(hud, ref used, frame, "PRESS P TO RESUME", 0f, -0.1f, PromptTextSize, PromptColor);
                break;
            case GamePhase.GameOver:
                DrawText(hud, ref used, frame, "GAME OVER", 0f, 0.3f, HeadlineTextSize, GameOverColor);
                if (blinkOn)
                    DrawText(hud, ref used, frame, "PRESS ENTER TO RETRY", 0f, -0.1f, PromptTextSize, PromptColor);
                break;
            case GamePhase.LevelComplete:
                DrawText(hud, ref used, frame, "LEVEL COMPLETE!", 0f, 0.5f, HeadlineTextSize, CompleteColor);
                DrawText(hud, ref used, frame, $"CRYSTALS {state.TotalCrystalsCollected}/{state.CrystalsInLevel}", 0f, 0.18f, PromptTextSize, CrystalColor);
                DrawText(hud, ref used, frame, $"CRATES {state.CratesBroken}/{state.CratesInLevel}", 0f, 0.04f, PromptTextSize, LivesColor);
                DrawText(hud, ref used, frame, $"TIME {FormatTime(state.LevelTime)}", 0f, -0.1f, PromptTextSize, PromptColor);
                if (blinkOn)
                    DrawText(hud, ref used, frame, "PRESS ENTER TO PLAY AGAIN", 0f, -0.45f, SmallTextSize, PromptColor);
                break;
        }

        if (state.MessageTime > 0f && state.Phase is GamePhase.Playing or GamePhase.Dying)
        {
            DrawText(hud, ref used, frame, state.Message, 0f, 0.55f, FlashTextSize, FlashColor);
            if (state.SubMessage.Length > 0)
                DrawText(hud, ref used, frame, state.SubMessage, 0f, 0.4f, PromptTextSize, PromptColor);
        }

        for (var i = used; i < hud.UsedLastFrame; i++)
            SetVisible(hud.PixelIds[i], false);
        hud.UsedLastFrame = used;
    }

    private readonly record struct HudFrame(Vector3 CameraPosition, Quaternion Orientation, float HalfHeight, float Aspect)
    {
        public Vector3 Right => Vector3.Transform(Vector3.UnitX, Orientation);
        public Vector3 Up => Vector3.Transform(Vector3.UnitY, Orientation);
        public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Orientation);

        /// <summary>Screen point in HUD units (x in [-aspect, aspect], y in [-1, 1]) → world position on the HUD plane.</summary>
        public Vector3 ToWorld(float x, float y) =>
            CameraPosition + Forward * HudDistance + Right * (x * HalfHeight) + Up * (y * HalfHeight);
    }

    private void DrawText(HudComponent hud, ref int used, in HudFrame frame, string text, float x, float y, float pixelSize, Vector3 color, Align align = Align.Center)
    {
        var width = DotFont.TextWidth(text) * pixelSize;
        var left = align switch
        {
            Align.Left => x,
            Align.Right => x - width,
            _ => x - width * 0.5f
        };
        var top = y + DotFont.Height * pixelSize * 0.5f;
        var rotation = GameMath.ToEngineEuler(frame.Orientation);
        var world = pixelSize * frame.HalfHeight;

        foreach (var (column, row, length) in DotFont.Runs(text))
        {
            var cx = left + (column + length * 0.5f) * pixelSize;
            var cy = top - (row + 0.5f) * pixelSize;
            var pixel = Pixel(hud, used++);
            var transform = pixel.GetComponent<TransformComponent>();
            transform.Translation = frame.ToWorld(cx, cy);
            transform.Rotation = rotation;
            transform.Scale = new Vector3(world * (length - (1f - PixelFill)), world * PixelFill, world * PixelDepth);
            transform.Visible = true;
            var renderer = pixel.GetComponent<ModelRendererComponent>();
            renderer.Emissive = color;
        }
    }

    private Entity Pixel(HudComponent hud, int index)
    {
        if (index < hud.PixelIds.Count && context.Contains(hud.PixelIds[index]))
            return context.GetById(hud.PixelIds[index]);

        var pixel = Spawner.Create(context, "HudPixel", Vector3.Zero, levelOwned: false);
        pixel.AddComponent(new ModelRendererComponent
        {
            Color = PixelAlbedo,
            Roughness = 1f,
            EmissiveStrength = EmissiveStrength
        });
        if (index < hud.PixelIds.Count)
            hud.PixelIds[index] = pixel.Id;
        else
            hud.PixelIds.Add(pixel.Id);
        return pixel;
    }

    private void DrawIcons(HudComponent hud, in HudFrame frame, bool visible)
    {
        var manifest = IconManifest();
        hud.CrystalIconId = Icon(hud.CrystalIconId, manifest.Get("crystal"), "HudCrystal");
        hud.LivesIconId = Icon(hud.LivesIconId, manifest.Get("player"), "HudLives");

        var spin = GameMath.YawRotation(hud.Time * IconSpinRate);
        PlaceIcon(hud.CrystalIconId, manifest.Get("crystal"), CrystalHeight, frame, -frame.Aspect + 0.12f, 0.8f, spin, visible);
        var face = GameMath.YawRotation(MathF.Sin(hud.Time * 1.5f) * 0.5f);
        PlaceIcon(hud.LivesIconId, manifest.Get("player"), PlayerHeight, frame, frame.Aspect - 0.12f, 0.8f, face, visible);
    }

    // Nominal kit heights (meters) the manifest normalizes these models to.
    private const float CrystalHeight = 0.6f;
    private const float PlayerHeight = 1.3f;

    private int Icon(int id, ModelEntry model, string name)
    {
        if (context.Contains(id))
            return id;
        var icon = Spawner.Create(context, name, Vector3.Zero, levelOwned: false);
        icon.AddComponent(model.CreateRenderer());
        return icon.Id;
    }

    private void PlaceIcon(int id, ModelEntry model, float modelHeight, in HudFrame frame, float x, float y, Quaternion localRotation, bool visible)
    {
        var transform = context.GetById(id).GetComponent<TransformComponent>();
        var size = IconSize * frame.HalfHeight / modelHeight;
        var rotation = Quaternion.Concatenate(localRotation, frame.Orientation);
        var scale = model.Scale * size;
        transform.Translation = frame.ToWorld(x, y - IconSize * 0.5f) + Vector3.Transform(model.Pivot * scale, rotation);
        transform.Rotation = GameMath.ToEngineEuler(rotation);
        transform.Scale = scale;
        transform.Visible = visible;
    }

    private AssetManifest? _iconManifest;

    private AssetManifest IconManifest() =>
        _iconManifest ??= AssetManifest.Load(World.Get<GameSettingsComponent>(context).ManifestPath);

    private void SetVisible(int id, bool visible)
    {
        if (context.Contains(id))
            context.GetById(id).GetComponent<TransformComponent>().Visible = visible;
    }

    private static string FormatTime(float seconds)
    {
        var total = (int)seconds;
        return $"{total / 60}:{total % 60:00}";
    }
}
