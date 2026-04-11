using System.Numerics;
using DoomGame.Map;
using DoomGame.Multiplayer;
using DoomGame.Player;
using DoomGame.Rendering;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using ImGuiNET;

namespace DoomGame;

public class DoomGameLayer : ILayer
{
    private static readonly Vector4[] WallColors =
    [
        Vector4.Zero,
        new(0.65f, 0.65f, 0.65f, 1f),  // 1 gray
        new(0.75f, 0.40f, 0.10f, 1f),  // 2 brown
        new(0.15f, 0.60f, 0.15f, 1f),  // 3 green
        new(0.15f, 0.25f, 0.75f, 1f),  // 4 blue
        new(0.75f, 0.10f, 0.10f, 1f),  // 5 red
    ];

    private readonly IGraphics2D _graphics2D;
    private readonly NetworkManager _network;

    private ScreenCamera _screenCamera = null!;
    private GameMap _map = null!;
    private LocalPlayer _localPlayer = null!;
    private RaycastRenderer _raycastRenderer = null!;

    private int _screenWidth = 1280;
    private int _screenHeight = 720;

    // Shoot flash timer for visual feedback
    private float _shootFlash;

    public DoomGameLayer(IGraphics2D graphics2D, NetworkManager network)
    {
        _graphics2D = graphics2D;
        _network = network;
    }

    public void OnAttach(IInputSystem inputSystem)
    {
        _screenCamera = new ScreenCamera(_screenWidth, _screenHeight);
        _map = new GameMap();
        _localPlayer = new LocalPlayer();
        _raycastRenderer = new RaycastRenderer();
    }

    public void OnDetach()
    {
        _network.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _localPlayer.Update(timeSpan, _map);
        _network.Update(timeSpan, _localPlayer);

        if (_localPlayer.JustShot)
            _shootFlash = 0.1f;
        if (_shootFlash > 0f)
            _shootFlash -= (float)timeSpan.TotalSeconds;

        _graphics2D.SetClearColor(new Vector4(0f, 0f, 0f, 1f));
        _graphics2D.Clear();
        _graphics2D.BeginScene(_screenCamera);

        RenderCeilingAndFloor();
        RenderWalls();
        RenderMinimap();
        RenderCrosshair();

        _graphics2D.EndScene();
    }

    public void Draw()
    {
        RenderHud();
    }

    public void HandleInputEvent(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case KeyPressedEvent pressed:
                _localPlayer.OnKeyPressed(pressed.KeyCode);
                break;
            case KeyReleasedEvent released:
                _localPlayer.OnKeyReleased(released.KeyCode);
                break;
        }
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resize && resize.Width > 0 && resize.Height > 0)
        {
            _screenWidth = resize.Width;
            _screenHeight = resize.Height;
            _screenCamera.Resize(_screenWidth, _screenHeight);
        }
    }

    private void RenderCeilingAndFloor()
    {
        float halfH = _screenHeight * 0.5f;

        // Ceiling (dark blue-gray)
        _graphics2D.DrawQuad(
            new Vector3(0f, halfH * 0.5f, 0f),
            new Vector2(_screenWidth, halfH),
            new Vector4(0.10f, 0.10f, 0.20f, 1f));

        // Floor (dark brown-gray)
        _graphics2D.DrawQuad(
            new Vector3(0f, -halfH * 0.5f, 0f),
            new Vector2(_screenWidth, halfH),
            new Vector4(0.18f, 0.14f, 0.10f, 1f));
    }

    private void RenderWalls()
    {
        var hits = _raycastRenderer.CastRays(
            _localPlayer.Position,
            _localPlayer.Direction,
            _localPlayer.CameraPlane,
            _map,
            _screenWidth);

        for (var x = 0; x < _screenWidth; x++)
        {
            var hit = hits[x];
            int lineHeight = System.Math.Min((int)(_screenHeight / hit.Distance), _screenHeight * 4);

            var baseColor = WallColors[System.Math.Clamp(hit.WallType, 0, WallColors.Length - 1)];

            // Y-side walls are darker for visual depth
            float sideDim = hit.Side == 1 ? 0.65f : 1.0f;

            // Distance fog
            float fog = 1.0f - System.Math.Clamp(hit.Distance / 14f, 0f, 0.85f);

            float r = baseColor.X * sideDim * fog;
            float g = baseColor.Y * sideDim * fog;
            float b = baseColor.Z * sideDim * fog;
            var color = new Vector4(r, g, b, 1f);

            // Shoot flash tints the scene red
            if (_shootFlash > 0f)
            {
                float t = _shootFlash / 0.1f * 0.3f;
                color = new Vector4(color.X + t, color.Y, color.Z, 1f);
            }

            float worldX = x - _screenWidth * 0.5f + 0.5f;

            _graphics2D.DrawQuad(
                new Vector3(worldX, 0f, 0.5f),
                new Vector2(1f, lineHeight),
                color);
        }
    }

    private void RenderMinimap()
    {
        const float MapScale = 5f;
        const float MapPad = 10f;

        float halfW = _screenWidth * 0.5f;
        float halfH = _screenHeight * 0.5f;

        // Anchor: bottom-left corner
        float originX = -halfW + MapPad + GameMap.Width * MapScale * 0.5f;
        float originY = -halfH + MapPad + GameMap.Height * MapScale * 0.5f;

        // Dark background panel
        _graphics2D.DrawQuad(
            new Vector3(originX, originY, 0.7f),
            new Vector2(GameMap.Width * MapScale + 4f, GameMap.Height * MapScale + 4f),
            new Vector4(0f, 0f, 0f, 0.7f));

        // Map cells
        for (int y = 0; y < GameMap.Height; y++)
        {
            for (int x = 0; x < GameMap.Width; x++)
            {
                int cell = _map.GetCell(x, y);
                if (cell == 0) continue;

                var color = WallColors[System.Math.Clamp(cell, 0, WallColors.Length - 1)];
                color = new Vector4(color.X * 0.8f, color.Y * 0.8f, color.Z * 0.8f, 1f);

                _graphics2D.DrawQuad(
                    new Vector3(originX + (x - GameMap.Width * 0.5f) * MapScale, originY + (y - GameMap.Height * 0.5f) * MapScale, 0.75f),
                    new Vector2(MapScale - 0.5f, MapScale - 0.5f),
                    color);
            }
        }

        // Local player dot (green)
        var pos = _localPlayer.Position;
        _graphics2D.DrawQuad(
            new Vector3(originX + (pos.X - GameMap.Width * 0.5f) * MapScale, originY + (pos.Y - GameMap.Height * 0.5f) * MapScale, 0.8f),
            new Vector2(MapScale * 0.9f, MapScale * 0.9f),
            new Vector4(0.0f, 1.0f, 0.0f, 1f));

        // Player direction indicator
        var dir = _localPlayer.Direction;
        _graphics2D.DrawLine(
            new Vector3(originX + (pos.X - GameMap.Width * 0.5f) * MapScale, originY + (pos.Y - GameMap.Height * 0.5f) * MapScale, 0.85f),
            new Vector3(originX + (pos.X + dir.X * 1.5f - GameMap.Width * 0.5f) * MapScale, originY + (pos.Y + dir.Y * 1.5f - GameMap.Height * 0.5f) * MapScale, 0.85f),
            new Vector4(0f, 1f, 0f, 1f),
            -1);

        // Remote players (red)
        foreach (var remote in _network.GetRemotePlayers())
        {
            _graphics2D.DrawQuad(
                new Vector3(originX + (remote.Position.X - GameMap.Width * 0.5f) * MapScale, originY + (remote.Position.Y - GameMap.Height * 0.5f) * MapScale, 0.8f),
                new Vector2(MapScale * 0.9f, MapScale * 0.9f),
                new Vector4(1.0f, 0.2f, 0.2f, 1f));
        }
    }

    private void RenderCrosshair()
    {
        const float CrossSize = 8f;
        const float CrossThick = 1.5f;
        var white = new Vector4(1f, 1f, 1f, 0.9f);

        // Horizontal bar
        _graphics2D.DrawQuad(
            new Vector3(0f, 0f, 0.9f),
            new Vector2(CrossSize * 2f, CrossThick),
            white);

        // Vertical bar
        _graphics2D.DrawQuad(
            new Vector3(0f, 0f, 0.9f),
            new Vector2(CrossThick, CrossSize * 2f),
            white);
    }

    private void RenderHud()
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10));
        ImGui.SetNextWindowSize(new Vector2(200, 90));
        ImGui.SetNextWindowBgAlpha(0.5f);
        ImGui.Begin("##hud", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                              ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar |
                              ImGuiWindowFlags.NoInputs);

        ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"HP: {_localPlayer.Health}");
        ImGui.TextColored(new Vector4(1f, 0.9f, 0.3f, 1f), $"Ammo: {_localPlayer.Ammo}");

        string modeLabel = _network.Mode switch
        {
            NetworkMode.Server => $"Server  ID:{_network.LocalPlayerId}",
            NetworkMode.Client => $"Client  ID:{_network.LocalPlayerId}",
            _ => "Offline",
        };
        ImGui.TextColored(new Vector4(0.6f, 0.9f, 1f, 1f), modeLabel);

        int remoteCount = _network.GetRemotePlayers().Count;
        if (remoteCount > 0)
            ImGui.TextColored(new Vector4(1f, 1f, 0.5f, 1f), $"Players online: {remoteCount + 1}");

        ImGui.End();

        ImGui.SetNextWindowPos(new Vector2(10, _screenHeight - 70f));
        ImGui.SetNextWindowSize(new Vector2(320, 60));
        ImGui.SetNextWindowBgAlpha(0.35f);
        ImGui.Begin("##controls", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                                   ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar |
                                   ImGuiWindowFlags.NoInputs);
        ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "W/S: Move   A/D: Turn   Q/E: Strafe   Space: Shoot");
        ImGui.End();
    }

}
