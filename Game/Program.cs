// See https://aka.ms/new-console-template for more information

using Game;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using OpenTK.Windowing.Desktop;


// // This line creates a new instance, and wraps the instance in a using statement so it's automatically disposed once we've exited the block.
// using Game game = new Game(800, 600, "LearnOpenTK");
// game.Run();
//
// public class Game : GameWindow
// {
//     public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(
//         gameWindowSettings, nativeWindowSettings)
//     {
//     }
//
//     public Game(int width, int height, string title) : base(GameWindowSettings.Default,
//         new NativeWindowSettings { Size = (width, height), Title = title })
//     {
//     }
// }

var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
logger.LogInformation("Program has started.");

var app = Sandbox.CreateApplication();
app.Run();

