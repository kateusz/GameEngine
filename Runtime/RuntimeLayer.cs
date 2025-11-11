using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Renderer;
using Engine.Scene;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Serilog;

namespace Runtime;

/// <summary>
/// Main layer for running published games.
/// Loads the startup scene and runs it without editor overhead.
/// </summary>
public class RuntimeLayer : ILayer
{
    private static readonly ILogger Logger = Log.ForContext<RuntimeLayer>();

    private readonly IGraphics2D _graphics2D;
    private readonly IGraphics3D _graphics3D;
    private readonly ISceneSerializer _sceneSerializer;
    private readonly RuntimeConfig _config;
    private readonly SceneFactory _sceneFactory;
    private Scene? _activeScene;
    private bool _isInitialized = false;

    public RuntimeLayer(
        IGraphics2D graphics2D,
        IGraphics3D graphics3D,
        ISceneSerializer sceneSerializer,
        RuntimeConfig config,
        SceneFactory sceneFactory)
    {
        _graphics2D = graphics2D;
        _graphics3D = graphics3D;
        _sceneSerializer = sceneSerializer;
        _config = config;
        _sceneFactory = sceneFactory;
    }

    public void OnAttach(IInputSystem inputSystem)
    {
        Logger.Information("RuntimeLayer attaching...");

        try
        {
            // Try to load pre-compiled scripts first (GameScripts.dll)
            var gameScriptsPath = Path.Combine(AppContext.BaseDirectory, "GameScripts.dll");
            if (File.Exists(gameScriptsPath))
            {
                Logger.Information("Found pre-compiled scripts: {GameScriptsPath}", gameScriptsPath);
                if (ScriptEngine.Instance.LoadPrecompiledAssembly(gameScriptsPath))
                {
                    Logger.Information("Pre-compiled scripts loaded successfully");
                }
                else
                {
                    Logger.Warning("Failed to load pre-compiled scripts, will try runtime compilation");
                }
            }
            else
            {
                // Fallback to runtime compilation (development mode)
                Logger.Information("No pre-compiled scripts found, using runtime compilation");

                var gameDataPath = Path.Combine(AppContext.BaseDirectory, "GameData");
                var scriptsPath = Path.Combine(gameDataPath, "scripts");

                // Fallback to assets/scripts if GameData doesn't exist
                if (!Directory.Exists(scriptsPath))
                {
                    scriptsPath = Path.Combine(AppContext.BaseDirectory, "assets", "scripts");
                }

                if (Directory.Exists(scriptsPath))
                {
                    ScriptEngine.Instance.SetScriptsDirectory(scriptsPath);
                    Logger.Information("Scripts directory set to: {ScriptsPath}", scriptsPath);
                }
                else
                {
                    Logger.Warning("Scripts directory not found: {ScriptsPath}", scriptsPath);
                }
            }

            // Load startup scene
            LoadStartupScene();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to initialize runtime layer");
            throw;
        }
    }

    private void LoadStartupScene()
    {
        var scenePath = _config.StartupScene;

        // Try to resolve scene path
        var fullPath = ResolveScenePath(scenePath);

        if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Startup scene not found: {scenePath}");
        }

        Logger.Information("Loading startup scene: {ScenePath}", fullPath);

        try
        {
            var sceneJson = File.ReadAllText(fullPath);
            _activeScene = _sceneSerializer.Deserialize(sceneJson, _sceneFactory);

            if (_activeScene != null)
            {
                CurrentScene.Instance = _activeScene;
                Logger.Information("Scene loaded successfully: {SceneName}", _activeScene.Name);
            }
            else
            {
                throw new InvalidOperationException("Failed to deserialize scene");
            }
        }
        catch (Exception ex)
        {
            Logger.Fatal(ex, "Failed to load startup scene");
            throw;
        }
    }

    private string? ResolveScenePath(string scenePath)
    {
        // If it's an absolute path and exists, use it
        if (Path.IsPathRooted(scenePath) && File.Exists(scenePath))
        {
            return scenePath;
        }

        // Try in GameData directory
        var gameDataPath = Path.Combine(AppContext.BaseDirectory, "GameData", scenePath);
        if (File.Exists(gameDataPath))
        {
            return gameDataPath;
        }

        // Try in GameData/scenes directory
        gameDataPath = Path.Combine(AppContext.BaseDirectory, "GameData", "scenes", scenePath);
        if (File.Exists(gameDataPath))
        {
            return gameDataPath;
        }

        // Try in assets directory (development mode)
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "assets", scenePath);
        if (File.Exists(assetsPath))
        {
            return assetsPath;
        }

        // Try in assets/scenes directory
        assetsPath = Path.Combine(AppContext.BaseDirectory, "assets", "scenes", scenePath);
        if (File.Exists(assetsPath))
        {
            return assetsPath;
        }

        return null;
    }

    public void OnDetach()
    {
        Logger.Information("RuntimeLayer detaching...");
        _activeScene = null;
        CurrentScene.Instance = null;
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (!_isInitialized || _activeScene == null)
            return;

        try
        {
            // Update scripts
            ScriptEngine.Instance.OnUpdate(deltaTime);

            // Render scene
            _graphics2D.SetClearColor(new System.Numerics.Vector4(0.1f, 0.1f, 0.1f, 1.0f));
            _graphics2D.Clear();

            // Render 2D and 3D content
            _activeScene.Render(_graphics2D, _graphics3D);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error during scene update");
        }
    }

    public void HandleInputEvent(InputEvent inputEvent)
    {
        try
        {
            ScriptEngine.Instance.ProcessEvent(inputEvent);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error handling input event");
        }
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        try
        {
            ScriptEngine.Instance.ProcessEvent(windowEvent);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error handling window event");
        }
    }

    public void Draw()
    {
        // Nothing to draw here - all rendering happens in OnUpdate
    }
}
