using System.Numerics;
using ECS.Systems;
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
    GameConfiguration gameConfig,
    ISystemManager systemManager,
    IEnumerable<IGameSystem> gameSystems)
    : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    // Named delegate so it can be unsubscribed in OnDetach
    private readonly Action<IScene> _sceneChangedHandler = _ => Logger.Information("Active scene changed");

    public void OnAttach(IInputSystem inputSystem)
    {
        foreach (var gameSystem in gameSystems)
        {
            systemManager.RegisterSystem(gameSystem);
        }
        
        sceneContext.SceneChanged += _sceneChangedHandler;

        Logger.Information("Game layer attached.");

        var scriptsDir = Path.Combine(AppContext.BaseDirectory, "assets", "scripts");
        var rel = string.IsNullOrWhiteSpace(gameConfig.GameAssemblyPath)
            ? "GameAssembly.dll"
            : gameConfig.GameAssemblyPath;
        var gameDll = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, rel));
        if (File.Exists(gameDll))
            scriptEngine.LoadGameAssemblyFromFile(gameDll, scriptsDir);
        else
            scriptEngine.SetScriptsDirectory(scriptsDir);

        // Load startup scene
        var startupScenePath = Path.Combine(AppContext.BaseDirectory, gameConfig.StartupScenePath);

        if (!File.Exists(startupScenePath))
        {
            Logger.Error("Startup scene not found: {Path} (current directory: {Dir})", startupScenePath, AppContext.BaseDirectory);
            Logger.Warning("Creating empty scene as fallback...");

            // Create empty scene as fallback
            var emptyScene = sceneFactory.Create("", "");
            sceneContext.SetScene(emptyScene);
        }
        else
        {
            try
            {
                Logger.Information("Loading startup scene from: {Path}", startupScenePath);

                // Create and load scene
                var scene = sceneFactory.Create(startupScenePath, Path.GetFileNameWithoutExtension(startupScenePath));
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
                var emptyScene = sceneFactory.Create("", "");
                sceneContext.SetScene(emptyScene);
            }
        }
    }

    public void OnDetach()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;

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

    public void HandleInputEvent(InputEvent windowEvent)
    {
        // Forward input events to scripts so they can respond to keyboard/mouse input
        scriptEngine.ProcessEvent(windowEvent);
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
