using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;
using ImGuiNET;
using Serilog;

namespace Sandbox;

public class Sandbox3DLayer(
    IGraphics3D graphics3D,
    SceneFactory sceneFactory,
    ModelSceneImporter modelSceneImporter) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<Sandbox3DLayer>();

    private IScene? _scene;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;

    private const string ModelPath = "assets/models/BistroExterior.fbx";

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - loading scene");

        _scene = sceneFactory.Create("Sandbox3D", "Sandbox3D");

        if (!File.Exists(ModelPath))
        {
            Logger.Error("Model not found at {Path}", ModelPath);
            return;
        }

        Logger.Information("Loading model from {Path}...", ModelPath);
        var result = modelSceneImporter.Import(_scene, ModelPath, addDefaultLighting: true, addCamera: true);
        Logger.Information("Scene loaded: {MeshCount} mesh entities", result.MeshEntities.Count);

        _cameraEntity = result.CameraEntity;

        var startPos = new Vector3(-4.72f, 3.39f, 22.50f);
        var initialYaw = 0f;
        if (_cameraEntity != null)
        {
            var t = _cameraEntity.GetComponent<TransformComponent>();
            
            // todo: hardcoded
            t.Translation = startPos;

            // Compute yaw to look toward scene center
            var toCenter = result.SceneCenter - startPos;
            if (toCenter.LengthSquared() > 0.001f)
                initialYaw = MathF.Atan2(-toCenter.X, -toCenter.Z);
        }

        _cameraController = new PerspectiveCameraController(startPos, initialYaw);

        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        _cameraController?.OnUpdate(timeSpan);

        if (_cameraEntity != null && _cameraController != null)
        {
            var transform = _cameraEntity.GetComponent<TransformComponent>();
            transform.Translation = _cameraController.Position;
            transform.Rotation = new Vector3(_cameraController.Pitch, _cameraController.Yaw, 0);
        }

        _fpsTimer += (float)timeSpan.TotalSeconds;
        _fpsFrames++;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsTimer = 0f;
            _fpsFrames = 0;
        }

        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();

        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
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

        var fpsText = $"FPS: {_fps:F0}";
        var fpsSize = ImGui.CalcTextSize(fpsText);
        var fpsPos = new Vector2(io.DisplaySize.X - fpsSize.X - padding, padding);
        drawList.AddText(fpsPos, white, fpsText);

        if (_cameraController != null)
        {
            var p = _cameraController.Position;
            var posText = $"X: {p.X:F2}  Y: {p.Y:F2}  Z: {p.Z:F2}";
            var posSize = ImGui.CalcTextSize(posText);
            var posPos = new Vector2(io.DisplaySize.X - posSize.X - padding, fpsPos.Y + fpsSize.Y + 2f);
            drawList.AddText(posPos, white, posText);
        }
    }
}
