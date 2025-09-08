using DryIoc;
using Editor;
using Engine.Core;
using Engine.Core.Modules;
using Engine.Core.Window;
using Engine.Renderer;
using Engine.Scripting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

var container = new ModuleContainerBuilder()
    .RegisterModule<CoreModule>()
    .RegisterModule<RenderingModule>()
    .RegisterModule<EditorModule>()
    .Build();

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
var graphics2D = container.Resolve<IGraphics2D>();
var graphics3D = container.Resolve<IGraphics3D>();
var editor = new global::Editor.Editor(gameWindow, graphics2D, graphics3D);
editor.PushLayer(editorLayer);
editor.Run();