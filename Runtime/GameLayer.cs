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
    Func<IEnumerable<IGameSystem>> resolveGameSystems)
    : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<GameLayer>();

    private readonly Action<IScene> _sceneChangedHandler = _ => Logger.Information("Active scene changed");

    public void OnAttach(IInputSystem inputSystem)
    {
        sceneContext.SceneChanged += _sceneChangedHandler;

        Logger.Information("Game layer attached.");

        var startupScenePath = Path.Combine(AppContext.BaseDirectory, gameConfig.StartupScenePath);

        if (!File.Exists(startupScenePath))
        {
            Logger.Error("Startup scene not found: {Path} (current directory: {Dir})", startupScenePath, AppContext.BaseDirectory);
            Logger.Warning("Creating empty scene as fallback...");

            var emptyScene = sceneFactory.Create("", "");
            sceneContext.SetScene(emptyScene);
            RuntimeSceneStarter.Start(emptyScene, sceneContext, resolveGameSystems());
        }
        else
        {
            try
            {
                Logger.Information("Loading startup scene from: {Path}", startupScenePath);

                var scene = sceneFactory.Create(startupScenePath, Path.GetFileNameWithoutExtension(startupScenePath));
                sceneSerializer.Deserialize(scene, startupScenePath);
                sceneContext.SetScene(scene);
                RuntimeSceneStarter.Start(scene, sceneContext, resolveGameSystems());
                Logger.Information("Startup scene loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load startup scene: {Path}", startupScenePath);

                var emptyScene = sceneFactory.Create("", "");
                sceneContext.SetScene(emptyScene);
                RuntimeSceneStarter.Start(emptyScene, sceneContext, resolveGameSystems());
            }
        }
    }

    public void OnDetach()
    {
        sceneContext.SceneChanged -= _sceneChangedHandler;

        Logger.Information("Game layer detached.");

        sceneContext.ActiveScene?.OnRuntimeStop();
        sceneContext.ActiveScene?.Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
        if (sceneContext.ActiveScene == null)
            return;

        graphics2D.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        graphics2D.Clear();

        sceneContext.ActiveScene.OnUpdateRuntime(timeSpan);
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        scriptEngine.ProcessEvent(windowEvent);
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowResizeEvent resizeEvent)
        {
            Logger.Information("GameLayer: Window resized: {Width}x{Height}", resizeEvent.Width, resizeEvent.Height);
            sceneContext.ActiveScene?.OnViewportResize((uint)resizeEvent.Width, (uint)resizeEvent.Height);
        }
    }

    public void Draw() { }
}
