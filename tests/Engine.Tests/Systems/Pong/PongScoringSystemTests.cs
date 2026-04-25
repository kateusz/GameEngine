using System.Numerics;
using ECS;
using Engine.Scene.Components;
using Engine.Scene.Components.Pong;
using Engine.Scene.Systems.Pong;
using Shouldly;

namespace Engine.Tests.Systems.Pong;

public class PongScoringSystemTests
{
    [Fact]
    public void PongScoringSystem_BallBeyondLeftBoundary_ShouldIncreaseAiScoreAndResetBall()
    {
        // Arrange
        var context = new Context();
        var scoreEntity = CreateScoreEntity(context, 1, maxScore: 10);
        var ballEntity = CreateBall(context, 2, new Vector3(-12.0f, 1.0f, 0.0f), new Vector2(-4.0f, 2.0f));
        var system = new PongScoringSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        scoreEntity.GetComponent<ScoreComponent>().AiScore.ShouldBe(1);
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore.ShouldBe(0);
        ballEntity.GetComponent<TransformComponent>().Translation.ShouldBe(Vector3.Zero);
        ballEntity.GetComponent<BallComponent>().Velocity.X.ShouldBeGreaterThan(0.0f);
    }

    [Fact]
    public void PongScoringSystem_BallBeyondRightBoundary_ShouldIncreasePlayerScoreAndResetBall()
    {
        // Arrange
        var context = new Context();
        var scoreEntity = CreateScoreEntity(context, 1, maxScore: 10);
        var ballEntity = CreateBall(context, 2, new Vector3(12.0f, -1.0f, 0.0f), new Vector2(4.0f, -2.0f));
        var system = new PongScoringSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore.ShouldBe(1);
        scoreEntity.GetComponent<ScoreComponent>().AiScore.ShouldBe(0);
        ballEntity.GetComponent<TransformComponent>().Translation.ShouldBe(Vector3.Zero);
        ballEntity.GetComponent<BallComponent>().Velocity.X.ShouldBeLessThan(0.0f);
    }

    [Fact]
    public void PongScoringSystem_ScoreReachesMax_ShouldSetGameOver()
    {
        // Arrange
        var context = new Context();
        var scoreEntity = CreateScoreEntity(context, 1, maxScore: 2);
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore = 1;
        var ballEntity = CreateBall(context, 2, new Vector3(12.0f, 0.0f, 0.0f), new Vector2(3.0f, 1.0f));
        var system = new PongScoringSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore.ShouldBe(2);
        scoreEntity.GetComponent<ScoreComponent>().IsGameOver.ShouldBeTrue();
        ballEntity.GetComponent<TransformComponent>().Translation.ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void PongScoringSystem_GameOver_ShouldNotChangeScoreOrBallState()
    {
        // Arrange
        var context = new Context();
        var scoreEntity = CreateScoreEntity(context, 1, maxScore: 10, isGameOver: true);
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore = 4;
        scoreEntity.GetComponent<ScoreComponent>().AiScore = 6;
        var ballEntity = CreateBall(context, 2, new Vector3(12.0f, 0.0f, 0.0f), new Vector2(3.0f, 1.0f));
        var system = new PongScoringSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        scoreEntity.GetComponent<ScoreComponent>().PlayerScore.ShouldBe(4);
        scoreEntity.GetComponent<ScoreComponent>().AiScore.ShouldBe(6);
        ballEntity.GetComponent<TransformComponent>().Translation.ShouldBe(new Vector3(12.0f, 0.0f, 0.0f));
        ballEntity.GetComponent<BallComponent>().Velocity.ShouldBe(new Vector2(3.0f, 1.0f));
    }

    [Fact]
    public void PongScoringSystem_MultipleBallsOutOfBounds_ShouldCountOnlyOneScoreEventPerTick()
    {
        // Arrange
        var context = new Context();
        var scoreEntity = CreateScoreEntity(context, 1, maxScore: 10);
        CreateBall(context, 2, new Vector3(-12.0f, 0.0f, 0.0f), new Vector2(-2.0f, 0.0f));
        CreateBall(context, 3, new Vector3(12.0f, 0.0f, 0.0f), new Vector2(2.0f, 0.0f));
        var system = new PongScoringSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        var score = scoreEntity.GetComponent<ScoreComponent>();
        (score.PlayerScore + score.AiScore).ShouldBe(1);
    }

    private static Entity CreateScoreEntity(Context context, int id, int maxScore, bool isGameOver = false)
    {
        var entity = Entity.Create(id, $"score-{id}");
        entity.AddComponent(new ScoreComponent { MaxScore = maxScore, IsGameOver = isGameOver });
        context.Register(entity);
        return entity;
    }

    private static Entity CreateBall(Context context, int id, Vector3 translation, Vector2 velocity)
    {
        var entity = Entity.Create(id, $"ball-{id}");
        entity.AddComponent(new TransformComponent { Translation = translation });
        entity.AddComponent(new BallComponent { Velocity = velocity, Speed = 8.0f });
        context.Register(entity);
        return entity;
    }
}
