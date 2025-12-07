using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Runtime;

public class GameLayer(
    IGraphics2D graphics2D,
    ISceneContext sceneContext,
    SceneFactory sceneFactory,
    ISceneSerializer sceneSerializer,
    IScriptEngine scriptEngine,
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

        // Set scripts directory for script engine
        var scriptsDir = Path.Combine(AppContext.BaseDirectory, "scripts");
        scriptEngine.SetScriptsDirectory(scriptsDir);

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
            // No scene loaded
            return;
        }

        // Clear the screen before rendering the new frame
        graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        graphics2D.Clear();

        // Update scene systems (this runs all ECS systems including rendering, physics, scripting, etc.)
        sceneContext.ActiveScene.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent inputEvent)
    {
        // Forward input events to scripts so they can respond to keyboard/mouse input
        scriptEngine.ProcessEvent(inputEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        // Handle window resize to update scene viewport
        if (windowEvent is WindowResizeEvent resizeEvent)
        {
            Logger.Information("GameLayer: Window resized: {Width}x{Height}", resizeEvent.Width, resizeEvent.Height);
            sceneContext.ActiveScene?.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw()
    {
        // Drawing is handled by OnUpdate through the scene's rendering systems
    }
}
