using DryIoc;
using Editor;
using Editor.Components;
using Editor.State;
using Engine.Core.Window;
using Engine.Renderer.Cameras;
using Engine.Scripting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

// Use logical size (half of physical pixels) for proper Retina handling
var props = new WindowProps("Sandbox Engine testing!", 1280, 800);

var container = new Container();

var options = WindowOptions.Default;
options.Size = new Vector2D<int>(props.Width, props.Height);
options.Title = "Game Window";

container.Register<IWindow>(Reuse.Singleton, 
    made: Made.Of(() => Window.Create(options))
);

container.Register<IGameWindow>(Reuse.Singleton, 
    made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
);

// Register EditorLayer dependencies
container.Register<EditorState>(Reuse.Singleton);
container.Register<EditorViewportState>(Reuse.Singleton);
container.Register<IEditorViewport, EditorViewport>(Reuse.Singleton);
container.Register<IEditorUIRenderer, EditorUIRenderer>(Reuse.Singleton);
container.Register<IEditorPerformanceMonitor, EditorPerformanceMonitor>(Reuse.Singleton);
container.Register<Workspace>(Reuse.Singleton);
container.Register<ProjectController>(Reuse.Singleton);
container.Register<SceneController>(Reuse.Singleton);
container.Register<EditorInputHandler>(Reuse.Singleton);
// Use proper aspect ratio for MacBook Pro 16" (16:10 aspect ratio)
container.Use(new OrthographicCameraController(1280.0f / 800.0f, true));

// Register EditorLayer with constructor injection
container.Register<EditorLayer>(Reuse.Singleton);

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

var gameWindow = container.Resolve<IGameWindow>();
var editorLayer = container.Resolve<EditorLayer>();
var editor = new global::Editor.Editor(gameWindow);
editor.PushLayer(editorLayer);
editor.Run();