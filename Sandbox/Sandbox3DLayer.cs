using System.Numerics;
using ECS;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Components;
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

    private const string ModelPath = "assets/models/BistroExterior.fbx";

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("Sandbox3DLayer OnAttach - loading scene");

        _scene = sceneFactory.Create("Sandbox3D");

        if (!File.Exists(ModelPath))
        {
            Logger.Error("Model not found at {Path}", ModelPath);
            return;
        }

        Logger.Information("Loading model from {Path}...", ModelPath);
        var result = modelSceneImporter.Import(_scene, ModelPath, addDefaultLighting: true, addCamera: true);
        Logger.Information("Scene loaded: {MeshCount} mesh entities", result.MeshEntities.Count);

        _cameraEntity = result.CameraEntity;

        var startPos = new Vector3(0, 5, 15);
        var initialYaw = 0f;
        if (_cameraEntity != null)
        {
            var t = _cameraEntity.GetComponent<TransformComponent>();
            startPos = t.Translation;

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

    public void OnUpdate(TimeSpan deltaTime)
    {
        _cameraController?.OnUpdate(deltaTime);

        if (_cameraEntity != null && _cameraController != null)
        {
            var transform = _cameraEntity.GetComponent<TransformComponent>();
            transform.Translation = _cameraController.Position;
            transform.Rotation = new Vector3(_cameraController.Pitch, _cameraController.Yaw, 0);
        }

        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();

        _scene?.OnUpdateRuntime(deltaTime);
    }

    public void HandleInputEvent(InputEvent inputEvent)
    {
        _cameraController?.OnEvent(inputEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
    }

    public void Draw() { }
}
