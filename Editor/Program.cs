using DryIoc;
using Editor;
using Editor.Managers;
using Editor.Panels.Elements;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Serilog;
using Editor.Logging;

static void ConfigureContainer(Container container)
{
    var props = new WindowProps("Editor", 1280, 720);
    var options = WindowOptions.Default;
    options.Size = new Vector2D<int>(props.Width, props.Height);
    options.Title = "Game Window";

    container.Register<IWindow>(Reuse.Singleton, 
        made: Made.Of(() => Window.Create(options))
    );

    container.Register<IGameWindow>(Reuse.Singleton, 
        made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
    );

    container.Register<ILayer, EditorLayer>(Reuse.Singleton);
    container.Register<IImGuiLayer, ImGuiLayer>(Reuse.Singleton);
    container.Register<IProjectManager, ProjectManager>(Reuse.Singleton);
    
    // Generic service resolver function
    container.RegisterDelegate<Func<Type, object>>(r => r.Resolve);
    
    container.Register<IPrefabSerializer, PrefabSerializer>(Reuse.Singleton);
    container.Register<IPrefabManager, PrefabManager>(Reuse.Singleton);
    container.Register<ISceneSerializer, SceneSerializer>(Reuse.Singleton);
    container.Register<Editor.Editor>(Reuse.Singleton);
    
    container.ValidateAndThrow();
}

var container = new Container();
ConfigureContainer(container);

// Configure Serilog early (before ConsolePanel)
// We'll reconfigure it after ConsolePanel is created
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.WithProperty("Application", "GameEngine")
    .Enrich.WithThreadId()
    .Enrich.With<FrameNumberEnricher>()
    .Enrich.With<SceneNameEnricher>()
    .WriteTo.Async(a => a.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .WriteTo.Async(a => a.File("logs/engine-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

Log.Information("Program has started.");

#if DEBUG
// Enable script debugging in debug builds
ScriptEngine.Instance.EnableHybridDebugging(true);
    
// Optional: Save debug symbols to disk for external debuggers
var symbolsPath = Path.Combine(Environment.CurrentDirectory, "DebugSymbols", "Scripts");
Directory.CreateDirectory(symbolsPath);
ScriptEngine.Instance.SaveDebugSymbols(Path.Combine(symbolsPath, "DynamicScripts"));

ScriptEngine.Instance.PrintDebugInfo();
#endif

var editor = container.Resolve<Editor.Editor>();
var editorLayer = container.Resolve<ILayer>();
editor.PushLayer(editorLayer);
editor.Run();