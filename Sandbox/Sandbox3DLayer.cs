using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Renderer.Cameras;
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
    private IModel? _model;
    private PerspectiveCameraController? _cameraController;
    private Entity? _cameraEntity;
    private float _fps;

    // Hardcoded path to Bistro model - change this to match your local setup
    private const string BistroModelPath = "assets/models/Bistro/Exterior/BistroExterior.fbx";
    //private const string BistroModelPath = "assets/models/Bistro/Interior/BistroInterior_Wine.fbx";
    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - loading Bistro scene");

        graphics3D.Init();

        _scene = sceneFactory.Create("Bistro");

        var modelPath = Path.Combine(AppContext.BaseDirectory, BistroModelPath);
        if (!File.Exists(modelPath))
        {
            Logger.Error("Bistro model not found at {Path}. " +
                         "Place BistroExterior.fbx in Sandbox/assets/models/Bistro/Exterior/",
                modelPath);
            return;
        }

        Logger.Information("Loading Bistro model from {Path}...", modelPath);
        var result = modelSceneImporter.Import(_scene, modelPath, addDefaultLighting: true, addCamera: true);
        Logger.Information("Bistro scene loaded: {MeshCount} meshes", result.MeshEntities.Count);

        _cameraEntity = result.CameraEntity;

        // Initialize perspective camera at the scene camera's starting position
        var startPos = new Vector3(0, 5, 15);
        if (_cameraEntity != null)
        {
            var t = _cameraEntity.GetComponent<TransformComponent>();
            startPos = t.Translation;
        }

        _cameraController = new PerspectiveCameraController(startPos);

        _scene.OnRuntimeStart();
    }

    public void OnDetach()
    {
        Logger.Information("Sandbox3DLayer OnDetach");
        _scene?.OnRuntimeStop();
        _scene?.Dispose();
        _model?.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds > 0)
            _fps = 1.0f / (float)timeSpan.TotalSeconds;

        _cameraController?.OnUpdate(timeSpan);

        // Sync controller state to the scene camera entity's transform
        if (_cameraEntity != null && _cameraController != null)
        {
            var transform = _cameraEntity.GetComponent<TransformComponent>();
            transform.Translation = _cameraController.Position;
            // Euler convention: X=rotation around X (pitch), Y=rotation around Y (yaw)
            transform.Rotation = new Vector3(_cameraController.Pitch, _cameraController.Yaw, 0);
        }

        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();

        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void Draw()
    {
        var io = ImGui.GetIO();
        var padding = 10.0f;
        var windowPos = new Vector2(io.DisplaySize.X - padding, padding);
        ImGui.SetNextWindowPos(windowPos, ImGuiCond.Always, new Vector2(1, 0));
        ImGui.SetNextWindowBgAlpha(0.0f);
        var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs |
                    ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove |
                    ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus;
        ImGui.Begin("##fps", flags);
        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{_fps:F0} FPS");
        ImGui.End();
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        _cameraController?.OnEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        _cameraController?.OnEvent(windowEvent);

        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
        {
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }
}
