using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer.Pipeline;
using Engine.Scene;
using ImGuiNET;
using Input;
using SceneComponents;
using SceneComponents.Camera;
using SceneComponents.Lighting;
using SceneComponents.Physics;
using SceneComponents.Rendering;
using Serilog;

namespace Sandbox;

public class BepuPhysics3DLayer(
    IGraphics3D graphics3D,
    SceneFactory sceneFactory) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<BepuPhysics3DLayer>();
    private static readonly Vector3 FloorSize = new(80f, 1f, 80f);
    private static readonly Vector4 PyramidColorLow = new(0.85f, 0.28f, 0.16f, 1f);
    private static readonly Vector4 PyramidColorHigh = new(0.95f, 0.78f, 0.32f, 1f);
    private static readonly Vector4 BulletColor = new(0.55f, 0.35f, 0.9f, 1f);
    private static readonly Vector4 WreckingColor = new(0.75f, 0.15f, 0.85f, 1f);

    private const int PyramidCount = 3;
    private const int RowCount = 8;

    private readonly List<Entity> _spawned = [];
    private readonly Random _random = new(5);

    private IScene? _scene;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("BepuPhysics3DLayer OnAttach — pyramid demo (Z shoot, X wrecking ball, R reset)");

        _scene = sceneFactory.Create("SandboxBepu3D", "SandboxBepu3D", SceneDimension.ThreeD);
        CreateCamera();
        CreateLights();
        CreateFloorEntity();
        BuildPyramids();

        _cameraController = new PerspectiveCameraController(new Vector3(0f, 10f, 24f), 0f, -0.15f);
        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _scene = null;
        _spawned.Clear();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController?.OnUpdate(timeSpan);
        SyncCamera();
        TickFps((float)timeSpan.TotalSeconds);

        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();
        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        if (windowEvent is KeyPressedEvent { IsRepeat: false } pressed)
        {
            if (pressed.KeyCode == KeyCodes.Z)
                LaunchBox(size: 0.5f + 2f * _random.NextSingle(), speed: 50f, BulletColor);
            else if (pressed.KeyCode == KeyCodes.X)
                LaunchBox(size: 3f, speed: 35f, WreckingColor);
            else if (pressed.KeyCode == KeyCodes.R)
                ResetDemo();
        }

        _cameraController?.OnEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
    }

    public void Draw()
    {
        const float padding = 10f;
        var io = ImGui.GetIO();
        var drawList = ImGui.GetForegroundDrawList();
        var white = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 1f));

        DrawHudLine(drawList, io.DisplaySize.X, padding, white, $"FPS: {_fps:F0}", out var y);
        if (_cameraController != null)
        {
            var p = _cameraController.Position;
            DrawHudLine(drawList, io.DisplaySize.X, y, white, $"X: {p.X:F2}  Y: {p.Y:F2}  Z: {p.Z:F2}", out y);
        }

        DrawHudLine(drawList, io.DisplaySize.X, y, white, $"Bodies: {_spawned.Count}", out _);

        var help = "Z shoot   X wrecking ball   R reset";
        drawList.AddText(new Vector2(padding, io.DisplaySize.Y - 28f), white, help);
    }

    private void ResetDemo()
    {
        foreach (var entity in _spawned)
            _scene?.DestroyEntity(entity);
        _spawned.Clear();
        BuildPyramids();
    }

    private void BuildPyramids()
    {
        const float boxSize = 1f;
        for (var pyramidIndex = 0; pyramidIndex < PyramidCount; pyramidIndex++)
        {
            for (var rowIndex = 0; rowIndex < RowCount; rowIndex++)
            {
                var columnCount = RowCount - rowIndex;
                var rowT = RowCount == 1 ? 0f : rowIndex / (float)(RowCount - 1);
                var color = Vector4.Lerp(PyramidColorLow, PyramidColorHigh, rowT);
                for (var columnIndex = 0; columnIndex < columnCount; columnIndex++)
                {
                    var position = new Vector3(
                        (-columnCount * 0.5f + columnIndex) * boxSize,
                        (rowIndex + 0.5f) * boxSize,
                        (pyramidIndex - PyramidCount * 0.5f) * (boxSize + 4f));
                    AddDynamicBox(position, Vector3.One, color, Vector3.Zero);
                }
            }
        }
    }

    private void LaunchBox(float size, float speed, Vector4 color)
    {
        if (_cameraController is null)
            return;

        var forward = _cameraController.Forward;
        var position = _cameraController.Position + forward * 3f;
        AddDynamicBox(position, new Vector3(size), color, forward * speed);
    }

    private void AddDynamicBox(Vector3 position, Vector3 scale, Vector4 color, Vector3 velocity)
    {
        var entity = _scene!.CreateEntity("Body");
        var transform = entity.AddComponent<TransformComponent>();
        transform.Translation = position;
        transform.Scale = scale;
        entity.AddComponent(new ModelRendererComponent(color));
        entity.AddComponent(new RigidBody3DComponent
        {
            BodyType = RigidBodyType.Dynamic,
            GravityScale = 1f,
            Velocity = velocity
        });
        entity.AddComponent(new BoxCollider3DComponent { Size = new Vector3(0.5f) });
        _spawned.Add(entity);
    }

    private void CreateCamera()
    {
        var cameraEntity = _scene!.CreateEntity("Camera");
        var cameraTransform = cameraEntity.AddComponent<TransformComponent>();
        cameraTransform.Translation = new Vector3(0f, 10f, 24f);
        var cameraComponent = cameraEntity.AddComponent<CameraComponent>();
        cameraComponent.Primary = true;
        cameraComponent.ProjectionType = CameraProjectionTypeData.Perspective;
        cameraComponent.PerspectiveFOV = MathF.PI / 4f;
        cameraComponent.PerspectiveNear = 0.1f;
        cameraComponent.PerspectiveFar = 400f;
        _cameraEntity = cameraEntity;
    }

    private void CreateLights()
    {
        var sky = _scene!.CreateEntity("Sky").AddComponent<SkyLightComponent>();
        sky.HdrPath = "assets/textures/skies/sky_1k.hdr";

        var sun = _scene.CreateEntity("Sun").AddComponent<DirectionalLightComponent>();
        sun.Direction = Vector3.Normalize(new Vector3(-0.4f, -0.8f, -0.3f));
        sun.Color = new Vector3(1f, 0.95f, 0.9f);
    }

    private void CreateFloorEntity()
    {
        var floor = _scene!.CreateEntity("Floor");
        var transform = floor.AddComponent<TransformComponent>();
        transform.Translation = new Vector3(0f, -0.5f, 0f);
        transform.Scale = FloorSize;
        floor.AddComponent(new ModelRendererComponent(new Vector4(0.45f, 0.45f, 0.5f, 1f)));
        floor.AddComponent(new RigidBody3DComponent { BodyType = RigidBodyType.Static, GravityScale = 0f });
        floor.AddComponent(new BoxCollider3DComponent { Size = new Vector3(0.5f) });
    }

    private void SyncCamera()
    {
        if (_cameraEntity is null || _cameraController is null)
            return;

        var transform = _cameraEntity.GetComponent<TransformComponent>();
        transform.Translation = _cameraController.Position;
        transform.Rotation = new Vector3(_cameraController.Pitch, _cameraController.Yaw, 0);
    }

    private void TickFps(float dt)
    {
        _fpsTimer += dt;
        _fpsFrames++;
        if (_fpsTimer < 0.5f)
            return;

        _fps = _fpsFrames / _fpsTimer;
        _fpsTimer = 0f;
        _fpsFrames = 0;
    }

    private static void DrawHudLine(ImDrawListPtr drawList, float displayWidth, float y, uint color, string text, out float nextY)
    {
        const float padding = 10f;
        var size = ImGui.CalcTextSize(text);
        drawList.AddText(new Vector2(displayWidth - size.X - padding, y), color, text);
        nextY = y + size.Y + 2f;
    }
}
