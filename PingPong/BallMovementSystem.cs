using ECS;
using ECS.Systems;
using SceneComponents;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class BallMovementSystem(IContext context) : IGameSystem
{
    public int Priority => 99;

    public void OnInit()
    {
        var ball = context.GetByName(PongEntityNames.Ball);
        if (ball is not null && !ball.HasComponent<BallComponent>())
            ball.AddComponent(new BallComponent { Speed = PongConstants.BallSpeed });

        var scoreEntity = context.GetByName(PongEntityNames.Score);
        if (scoreEntity is not null && !scoreEntity.HasComponent<ScoreComponent>())
            scoreEntity.AddComponent(new ScoreComponent { MaxScore = PongConstants.MaxScore });
    }

    public void OnUpdate(TimeSpan deltaTime)
    {
        if (IsGameOver())
            return;

        var deltaSeconds = (float)deltaTime.TotalSeconds;
        foreach (var (entity, ball) in context.View<BallComponent>())
        {
            if (!entity.TryGetComponent<TransformComponent>(out var transform))
                continue;

            if (ball.Velocity.LengthSquared() <= float.Epsilon)
                ball.Velocity = new System.Numerics.Vector2(ball.Speed, 0.0f);

            var translation = transform.Translation;
            translation.X += ball.Velocity.X * deltaSeconds;
            translation.Y += ball.Velocity.Y * deltaSeconds;
            transform.Translation = translation;
        }
    }

    public void OnShutdown() { }
}
