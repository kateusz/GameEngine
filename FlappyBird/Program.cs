using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace FlappyBird;

class Program
{
    static void Main(string[] args)
    {
        var logger = LoggerFactory.Create(builder => builder.AddNLog()).CreateLogger<Program>();
        logger.LogInformation("FlappyBirdGame has started.");

        var flappyBirdGame = new FlappyBirdGame();
        flappyBirdGame.PushLayer(new GameLayer("Game Layer"));
        flappyBirdGame.Run();
    }
}