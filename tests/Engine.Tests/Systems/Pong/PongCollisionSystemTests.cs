using System.Numerics;
using ECS;
using Engine.Scene.Components;
using Engine.Scene.Components.Pong;
using Engine.Scene.Systems.Pong;
using Shouldly;

namespace Engine.Tests.Systems.Pong;

public class PongCollisionSystemTests
{
    [Fact]
    public void PongCollisionSystem_WallHit_ShouldFlipVerticalVelocity()
    {
        // Arrange
        var context = new Context();
        CreateBoundary(context, 1, BoundaryPosition.Top, 5.0f);
        var ballEntity = CreateBall(context, 2, new Vector3(0.0f, 5.0f, 0.0f), new Vector2(2.0f, 3.0f));
        var system = new PongCollisionSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        ballEntity.GetComponent<BallComponent>().Velocity.ShouldBe(new Vector2(2.0f, -3.0f));
    }

    [Fact]
    public void PongCollisionSystem_BottomWallHit_ShouldFlipVerticalVelocityAndClampPosition()
    {
        // Arrange
        var context = new Context();
        CreateBoundary(context, 1, BoundaryPosition.Bottom, -5.0f);
        var ballEntity = CreateBall(context, 2, new Vector3(0.0f, -6.0f, 0.0f), new Vector2(1.0f, -3.0f));
        var system = new PongCollisionSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        var ball = ballEntity.GetComponent<BallComponent>();
        var transform = ballEntity.GetComponent<TransformComponent>();
        ball.Velocity.ShouldBe(new Vector2(1.0f, 3.0f));
        transform.Translation.Y.ShouldBe(-5.0f);
    }

    [Fact]
    public void PongCollisionSystem_PaddleHit_ShouldFlipHorizontalVelocity()
    {
        // Arrange
        var context = new Context();
        CreatePaddle(context, 1, new Vector3(2.0f, 1.0f, 0.0f));
        var ballEntity = CreateBall(context, 2, new Vector3(2.0f, 1.0f, 0.0f), new Vector2(4.0f, 0.0f));
        var system = new PongCollisionSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        ballEntity.GetComponent<BallComponent>().Velocity.ShouldBe(new Vector2(-4.0f, 0.0f));
    }

    [Fact]
    public void PongCollisionSystem_OffCenterPaddleHit_ShouldAddVerticalVariation()
    {
        // Arrange
        var context = new Context();
        CreatePaddle(context, 1, new Vector3(2.0f, 1.0f, 0.0f));
        var ballEntity = CreateBall(context, 2, new Vector3(2.0f, 2.2f, 0.0f), new Vector2(4.0f, 0.0f));
        var system = new PongCollisionSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        var newVelocity = ballEntity.GetComponent<BallComponent>().Velocity;
        newVelocity.X.ShouldBeLessThan(0.0f);
        newVelocity.Y.ShouldNotBe(0.0f);
    }

    [Fact]
    public void PongCollisionSystem_GameOver_ShouldNotUpdateCollisionState()
    {
        // Arrange
        var context = new Context();
        CreateBoundary(context, 1, BoundaryPosition.Top, 5.0f);
        CreatePaddle(context, 2, new Vector3(2.0f, 0.0f, 0.0f));
        CreateScore(context, 3, isGameOver: true);
        var ballEntity = CreateBall(context, 4, new Vector3(2.0f, 5.0f, 0.0f), new Vector2(4.0f, 2.0f));
        var system = new PongCollisionSystem(context);

        // Act
        system.OnUpdate(TimeSpan.FromSeconds(0.016));

        // Assert
        ballEntity.GetComponent<BallComponent>().Velocity.ShouldBe(new Vector2(4.0f, 2.0f));
    }

    private static Entity CreateBall(Context context, int id, Vector3 translation, Vector2 velocity)
    {
        var entity = Entity.Create(id, $"ball-{id}");
        entity.AddComponent(new TransformComponent { Translation = translation });
        entity.AddComponent(new BallComponent { Velocity = velocity });
        context.Register(entity);
        return entity;
    }

    private static void CreateBoundary(Context context, int id, BoundaryPosition position, float y)
    {
        var entity = Entity.Create(id, $"boundary-{id}");
        entity.AddComponent(new TransformComponent { Translation = new Vector3(0.0f, y, 0.0f) });
        entity.AddComponent(new BoundaryComponent { Position = position });
        context.Register(entity);
    }

    private static void CreatePaddle(Context context, int id, Vector3 translation)
    {
        var entity = Entity.Create(id, $"paddle-{id}");
        entity.AddComponent(new TransformComponent { Translation = translation });
        entity.AddComponent(new PaddleComponent());
        context.Register(entity);
    }

    private static void CreateScore(Context context, int id, bool isGameOver)
    {
        var entity = Entity.Create(id, $"score-{id}");
        entity.AddComponent(new ScoreComponent { IsGameOver = isGameOver });
        context.Register(entity);
    }
}
