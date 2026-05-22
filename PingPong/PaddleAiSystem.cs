using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class PaddleAiSystem(IContext context) : IGameSystem
{
    public int Priority => 102;

    public void OnInit()
    {
        var aiPaddle = context.GetByName(PongEntityNames.AiPaddle);
        if (aiPaddle is null || aiPaddle.HasComponent<PaddleComponent>())
            return;

        aiPaddle.AddComponent(new PaddleComponent
        {
            IsPlayer = false,
            MoveSpeed = PongConstants.AiMoveSpeed
        });
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
    }

    public void OnShutdown() { }
}
