using System.Numerics;
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
    IMeshFactory meshFactory,
    SceneFactory sceneFactory,
    ModelSceneImporter modelSceneImporter) : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<Sandbox3DLayer>();

    private IScene? _scene;
    private IModel? _model;

    // Hardcoded path to Bistro model - change this to match your local setup
    private const string BistroModelPath = "assets/models/Bistro/Exterior/BistroExterior.fbx";

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
        graphics3D.SetClearColor(new Vector4(0.1f, 0.1f, 0.15f, 1.0f));
        graphics3D.Clear();

        _scene?.OnUpdateRuntime(timeSpan);
    }

    public void Draw()
    {
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent && _scene != null)
        {
            _scene.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }
}
