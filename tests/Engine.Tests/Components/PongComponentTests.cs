using System.Numerics;
using Engine.Scene.Components.Pong;
using Shouldly;

namespace Engine.Tests.Components;

public class PongComponentTests
{
    [Fact]
    public void PaddleComponent_Defaults_ShouldMatchExpectedValues()
    {
        // Act
        var paddle = new PaddleComponent();

        // Assert
        paddle.IsPlayer.ShouldBeFalse();
        paddle.MoveSpeed.ShouldBe(6.0f);
    }

    [Fact]
    public void PaddleComponent_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new PaddleComponent { IsPlayer = true, MoveSpeed = 7.5f };

        // Act
        var clone = (PaddleComponent)original.Clone();
        clone.MoveSpeed = 3.0f;

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.IsPlayer.ShouldBeTrue();
        original.MoveSpeed.ShouldBe(7.5f);
        clone.MoveSpeed.ShouldBe(3.0f);
    }

    [Fact]
    public void BallComponent_Defaults_ShouldMatchExpectedValues()
    {
        // Act
        var ball = new BallComponent();

        // Assert
        ball.Velocity.ShouldBe(Vector2.Zero);
        ball.Speed.ShouldBe(8.0f);
    }

    [Fact]
    public void BallComponent_Clone_ShouldCopyValues()
    {
        // Arrange
        var original = new BallComponent { Velocity = new Vector2(1.5f, -2.5f), Speed = 9.5f };

        // Act
        var clone = (BallComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Velocity.ShouldBe(new Vector2(1.5f, -2.5f));
        clone.Speed.ShouldBe(9.5f);
    }

    [Fact]
    public void BoundaryComponent_Defaults_ShouldRepresentTopBoundary()
    {
        // Act
        var boundary = new BoundaryComponent();

        // Assert
        boundary.Position.ShouldBe(BoundaryPosition.Top);
    }

    [Fact]
    public void BoundaryComponent_Clone_ShouldCopyPosition()
    {
        // Arrange
        var original = new BoundaryComponent { Position = BoundaryPosition.Bottom };

        // Act
        var clone = (BoundaryComponent)original.Clone();

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.Position.ShouldBe(BoundaryPosition.Bottom);
    }

    [Fact]
    public void ScoreComponent_Defaults_ShouldMatchMilestoneRules()
    {
        // Act
        var score = new ScoreComponent();

        // Assert
        score.PlayerScore.ShouldBe(0);
        score.AiScore.ShouldBe(0);
        score.MaxScore.ShouldBe(10);
        score.IsGameOver.ShouldBeFalse();
    }

    [Fact]
    public void ScoreComponent_Clone_ShouldCreateIndependentCopy()
    {
        // Arrange
        var original = new ScoreComponent
        {
            PlayerScore = 3,
            AiScore = 4,
            MaxScore = 11,
            IsGameOver = true
        };

        // Act
        var clone = (ScoreComponent)original.Clone();
        clone.PlayerScore = 10;

        // Assert
        clone.ShouldNotBeSameAs(original);
        clone.PlayerScore.ShouldBe(10);
        original.PlayerScore.ShouldBe(3);
        clone.AiScore.ShouldBe(4);
        clone.MaxScore.ShouldBe(11);
        clone.IsGameOver.ShouldBeTrue();
    }
}
