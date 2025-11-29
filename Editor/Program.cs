using DryIoc;
using Editor;
using Editor.Panels;
using Engine.Core;
using Engine.ImGuiNet;
using Engine.Scripting;
using Serilog;
using Editor.Logging;
using Engine;

static void ConfigureContainer(Container container)
{
    EngineIoCContainer.Register(container);
    EditorIoCContainer.Register(container);
    
    container.ValidateAndThrow();
}

var container = new Container();
ConfigureContainer(container);

// Create ConsolePanel early so we can configure logging with it
var consolePanel = container.Resolve<IConsolePanel>();

// Configure Serilog with all sinks in one place
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.WithProperty("Application", "GameEngine")
    .Enrich.WithThreadId()
    .WriteTo.Async(a => a.ConsolePanel(consolePanel))
    .WriteTo.Async(a => a.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .WriteTo.Async(a => a.File("logs/engine-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"))
    .CreateLogger();

Log.Information("Program has started.");

#if DEBUG
// Enable script debugging in debug builds
var scriptEngine = container.Resolve<IScriptEngine>();
scriptEngine.EnableHybridDebugging(true);

// Optional: Save debug symbols to disk for external debuggers
var symbolsPath = Path.Combine(Environment.CurrentDirectory, "DebugSymbols", "Scripts");
Directory.CreateDirectory(symbolsPath);
scriptEngine.SaveDebugSymbols(Path.Combine(symbolsPath, "DynamicScripts"));

scriptEngine.PrintDebugInfo();
#endif

var editor = container.Resolve<Editor.Editor>();
var editorLayer = container.Resolve<ILayer>();
editor.PushLayer(editorLayer);
editor.Run();