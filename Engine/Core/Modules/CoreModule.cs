using DryIoc;
using Engine.Core.Window;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Core.Modules;

public class CoreModule : IModule
{
    public int Priority => 0;

    public void Register(IContainer container)
    {
        // Window management
        var props = new WindowProps("Engine", 1280, 720);
        var options = WindowOptions.Default;
        options.Size = new Vector2D<int>(props.Width, props.Height);
        options.Title = "Game Window";

        container.Register<IWindow>(Reuse.Singleton, 
            made: Made.Of(() => Silk.NET.Windowing.Window.Create(options))
        );
        container.Register<IGameWindow>(Reuse.Singleton, 
            made: Made.Of(() => GameWindowFactory.Create(Arg.Of<IWindow>()))
        );

        // Logging
        container.RegisterDelegate(
            typeof(ILoggerFactory),
            r => LoggerFactory.Create(builder => builder.AddNLog()),
            Reuse.Singleton
        );

        //container.Register<ILoggerFactory>(Reuse.Singleton,
        //    made: Made.Of(() => LoggerFactory.Create(builder => builder.AddNLog())));

        // Generic logger registration
        container.Register(typeof(ILogger<>), typeof(Logger<>), Reuse.Singleton);

    }
}