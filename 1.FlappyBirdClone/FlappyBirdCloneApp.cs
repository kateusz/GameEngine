using Engine;
using Engine.Core;

namespace _1.FlappyBirdClone;

public class FlappyBirdCloneApp : Application
{
    public FlappyBirdCloneApp()
    {
        PushLayer(new GameLayer("Game Layer"));
    }
}