using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Serializer;
using Serilog;

namespace Runtime;

public class GameLayer(
    IGraphics2D graphics2D,
    ISceneContext sceneContext,
    SceneFactory sceneFactory,
    ISceneSerializer sceneSerializer,
    GameConfiguration gameConfig)
    : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    public void OnAttach(IInputSystem inputSystem)
    {
        sceneContext.SceneChanged += _ =>
        {
            Logger.Information("Active scene changed");
        };
        
        Logger.Information("Game layer attached.");

        // Load startup scene
        var startupScenePath = Path.Combine(AppContext.BaseDirectory, gameConfig.StartupScenePath);

        if (!File.Exists(startupScenePath))
        {
            Logger.Error("Startup scene not found: {Path}", startupScenePath);
            Logger.Error("Current directory: {Dir}", AppContext.BaseDirectory);
            Logger.Warning("Creating empty scene as fallback...");

            // Create empty scene as fallback
            var emptyScene = sceneFactory.Create("");
            sceneContext.SetScene(emptyScene);
        }
        else
        {
            try
            {
                Logger.Information("Loading startup scene from: {Path}", startupScenePath);

                // Create and load scene
                var scene = sceneFactory.Create(startupScenePath);
                sceneSerializer.Deserialize(scene, startupScenePath);
                sceneContext.SetScene(scene);

                // Start runtime (activate systems, physics, etc.)
                scene.OnRuntimeStart();

                Logger.Information("Startup scene loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load startup scene: {Path}", startupScenePath);

                // Create empty scene as fallback
                var emptyScene = sceneFactory.Create("");
                sceneContext.SetScene(emptyScene);
            }
        }
    }

    public void OnDetach()
    {
        Logger.Information("Game layer detached.");

        // Stop runtime and cleanup
        sceneContext.ActiveScene?.OnRuntimeStop();
        sceneContext.ActiveScene?.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (sceneContext.ActiveScene == null)
        {
            // No scene loaded, just clear screen
            graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
            graphics2D.Clear();
            return;
        }

        // Update scene systems (this runs all ECS systems including rendering, physics, scripting, etc.)
        sceneContext.ActiveScene.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent inputEvent)
    {
        // Input events are handled by scripts via the Input system
        // No manual handling needed here
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        // Window events are handled by the engine
        // Camera control is handled by scripts
    }

    public void Draw()
    {
        // Drawing is handled by OnUpdate through the scene's rendering systems
    }
}
