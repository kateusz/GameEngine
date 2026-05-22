using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Physics;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class PaddleInputSystem(IContext context) : IGameSystem
{
    public int Priority => 101;

    public void OnInit()
    {
        var player = context.GetByName(PongEntityNames.Player);
        if (player is null || player.HasComponent<PaddleComponent>())
            return;

        player.AddComponent(new PaddleComponent
        {
            IsPlayer = true,
            MoveSpeed = PongConstants.PaddleMoveSpeed
        });
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (IsGameOver())
            return;

        var direction = 0.0f;
        if (PongInputState.MoveUpPressed)
            direction += 1.0f;
        if (PongInputState.MoveDownPressed)
            direction -= 1.0f;

        foreach (var (entity, paddle) in context.View<PaddleComponent>())
        {
            if (!paddle.IsPlayer)
                continue;
            if (!entity.TryGetComponent<RigidBody2DComponent>(out var rigidBody))
                continue;

            rigidBody.Velocity = new Vector2(0, direction * paddle.MoveSpeed);
        }
    }

    public void OnShutdown() { }

    private bool IsGameOver()
    {
        foreach (var (_, score) in context.View<ScoreComponent>())
        {
            if (score.IsGameOver)
                return true;
        }
        return false;
    }
}
