using DoomGame;
using DoomGame.Multiplayer;
using DryIoc;
using Engine.Core;
using Engine.Core.DI;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

// Usage:
//   DoomGame                          -- offline / solo
//   DoomGame --server [--port 7777]   -- host a game server
//   DoomGame --connect <ip> [--port 7777] -- join a server

var port = 7777;
var mode = NetworkMode.Offline;
var serverHost = "127.0.0.1";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--server":
            mode = NetworkMode.Server;
            break;
        case "--connect" when i + 1 < args.Length:
            mode = NetworkMode.Client;
            serverHost = args[++i];
            break;
        case "--port" when i + 1 < args.Length:
            port = int.Parse(args[++i]);
            break;
    }
}

var container = new Container();
try
{
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.WithProperty("Application", "DoomGame")
        .WriteTo.Async(a => a.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
            theme: ConsoleTheme.None))
        .CreateLogger();

    Log.Information("DoomGame starting — mode: {Mode}", mode);

    EngineIoCContainer.Register(container);

    container.Register<ECS.IContext, ECS.Context>(Reuse.Singleton);
    container.RegisterDelegate<NetworkManager>(_ => new NetworkManager(mode, serverHost, port), Reuse.Singleton);
    container.Register<ILayer, DoomGameLayer>(Reuse.Singleton);
    container.Register<DoomApplication>(Reuse.Singleton);

    container.ValidateAndThrow();

    var app = container.Resolve<DoomApplication>();
    var layer = container.Resolve<ILayer>();
    app.PushLayer(layer);
    app.Run();

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Fatal: {ex.GetType().Name}: {ex.Message}");
    Console.Error.WriteLine(ex.StackTrace);
    return 1;
}
finally
{
    Log.CloseAndFlush();
    container.Dispose();
}
