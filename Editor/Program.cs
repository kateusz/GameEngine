using DryIoc;
using Editor;
using Editor.Managers;
using Editor.Panels.Elements;
using Engine.Core;
using Engine.Core.Window;
using Engine.ImGuiNet;
using Engine.Scene.Serializer;
using Engine.Scripting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

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

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
logger.LogInformation("Program has started.");

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