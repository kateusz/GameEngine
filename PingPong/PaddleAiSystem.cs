using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Physics;
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
        if (IsGameOver())
            return;

        var ball = context.GetByName(PongEntityNames.Ball);
        if (ball?.TryGetComponent<TransformComponent>(out var ballTransform) != true)
            return;

        var ballY = ballTransform.Translation.Y;

        foreach (var (entity, paddle) in context.View<PaddleComponent>())
        {
            if (paddle.IsPlayer)
                continue;
            if (!entity.TryGetComponent<RigidBody2DComponent>(out var rigidBody))
                continue;
            if (!entity.TryGetComponent<TransformComponent>(out var paddleTransform))
                continue;

            var diff = ballY - paddleTransform.Translation.Y;
            var direction = MathF.Abs(diff) > PongConstants.AiVerticalDeadzone
                ? (diff > 0 ? 1.0f : -1.0f)
                : 0.0f;

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
