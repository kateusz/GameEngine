using Engine.Core;

namespace FlappyBird;

public class FlappyBirdGame : Application
{
    public FlappyBirdGame()
    {
        PushLayer(new GameLayer("Game Layer"));
    }
}