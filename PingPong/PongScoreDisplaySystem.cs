using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents;
using SceneComponents.Rendering;
using Scripting;

namespace PingPong;

[Register(typeof(IGameSystem))]
internal sealed class PongScoreDisplaySystem(IContext context) : IGameSystem
{
    private static readonly Vector4 DimmedTint = new(0.45f, 0.45f, 0.45f, 1.0f);
    private static readonly Vector4 NormalTint = new(1.0f, 1.0f, 1.0f, 1.0f);

    public int Priority => 107;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var scoreEntity = context.View<ScoreComponent>().FirstOrDefault();
        if (scoreEntity == default)
            return;

        var score = scoreEntity.Component;
        UpdateScoreBar(PongEntityNames.PlayerScoreBar, score.PlayerScore, score.MaxScore);
        UpdateScoreBar(PongEntityNames.AiScoreBar, score.AiScore, score.MaxScore);
        UpdateGameOverBanner(score.IsGameOver);
        UpdateGameplayTint(score.IsGameOver);
    }

    public void OnShutdown() { }

    private void UpdateScoreBar(string entityName, int points, int maxScore)
    {
        var entity = context.GetByName(entityName);
        if (entity is null ||
            !entity.TryGetComponent<TransformComponent>(out var transform) ||
            !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        var fill = maxScore > 0 ? points / (float)maxScore : 0.0f;
        transform.Scale = transform.Scale with
        {
            X = fill * PongConstants.ScoreBarMaxWidth
        };
        sprite.Color = sprite.Color with { W = fill > 0.0f ? 1.0f : 0.0f };
    }

    private void UpdateGameOverBanner(bool isGameOver)
    {
        var entity = context.GetByName(PongEntityNames.GameOverBanner);
        if (entity is null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        sprite.Color = sprite.Color with { W = isGameOver ? 0.85f : 0.0f };
    }

    private void UpdateGameplayTint(bool isGameOver)
    {
        var tint = isGameOver ? DimmedTint : NormalTint;
        ApplyTint(PongEntityNames.Ball, tint);
        ApplyTint(PongEntityNames.Player, tint);
        ApplyTint(PongEntityNames.AiPaddle, tint);
    }

    private void ApplyTint(string entityName, Vector4 tint)
    {
        var entity = context.GetByName(entityName);
        if (entity is null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        sprite.Color = sprite.Color with
        {
            X = tint.X,
            Y = tint.Y,
            Z = tint.Z
        };
    }
}
