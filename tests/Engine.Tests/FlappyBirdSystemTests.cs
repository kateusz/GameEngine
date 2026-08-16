using FlappyBird.assets.scripts;
using Shouldly;

namespace Engine.Tests;

public class FlappyBirdSystemTests
{
    [Fact]
    public void HitsAnyPipe_BirdCenteredInGap_ReturnsFalse()
    {
        var game = new FlappyBirdGameComponent { BirdX = -1.2f, BirdY = 0f };
        game.PipeX[0] = -1.2f;
        game.PipeGapY[0] = 0f;

        FlappyBirdSystem.HitsAnyPipe(game).ShouldBeFalse();
    }

    [Fact]
    public void HitsAnyPipe_BirdAboveGap_ReturnsTrue()
    {
        var game = new FlappyBirdGameComponent { BirdX = -1.2f, BirdY = 1.0f };
        game.PipeX[0] = -1.2f;
        game.PipeGapY[0] = 0f;

        FlappyBirdSystem.HitsAnyPipe(game).ShouldBeTrue();
    }

    [Fact]
    public void HitsAnyPipe_BirdOutsidePipeColumn_ReturnsFalse()
    {
        var game = new FlappyBirdGameComponent { BirdX = -1.2f, BirdY = 1.0f };
        game.PipeX[0] = 3.0f;
        game.PipeGapY[0] = 0f;

        FlappyBirdSystem.HitsAnyPipe(game).ShouldBeFalse();
    }

    [Fact]
    public void HitsGround_BirdBelowGroundTop_ReturnsTrue()
    {
        var game = new FlappyBirdGameComponent { BirdY = gameGroundY() - 0.5f };

        FlappyBirdSystem.HitsGround(game).ShouldBeTrue();

        static float gameGroundY() => new FlappyBirdGameComponent().GroundTopY;
    }

    [Fact]
    public void HitsGround_BirdAboveGround_ReturnsFalse()
    {
        var game = new FlappyBirdGameComponent { BirdY = 0f };

        FlappyBirdSystem.HitsGround(game).ShouldBeFalse();
    }

    [Fact]
    public void ScorePassedPipes_PipePastBird_ScoresOnce()
    {
        var game = new FlappyBirdGameComponent { BirdX = -1.2f };
        game.PipeX[0] = -2.0f;
        game.PipeX[1] = 5.0f;
        game.PipeX[2] = 8.0f;

        FlappyBirdSystem.ScorePassedPipes(game).ShouldBe(1);
        game.Score.ShouldBe(1);
        game.PipeScored[0].ShouldBeTrue();

        // Passing the same pipe again must not double-count.
        FlappyBirdSystem.ScorePassedPipes(game).ShouldBe(0);
        game.Score.ShouldBe(1);
    }

    [Fact]
    public void AdvancePipes_MovesLeftAndRecyclesOffScreenPipe()
    {
        var game = new FlappyBirdGameComponent();
        game.PipeX[0] = -4.9f;
        game.PipeX[1] = 1.0f;
        game.PipeX[2] = 2.0f;
        game.PipeScored[0] = true;

        FlappyBirdSystem.AdvancePipes(game, 1f);

        // Off-screen pipe wraps back to the right of the rightmost pipe.
        game.PipeX[0].ShouldBeGreaterThan(4.0f);
        game.PipeScored[0].ShouldBeFalse();

        // On-screen pipe just scrolls left by PipeSpeed * dt.
        game.PipeX[1].ShouldBe(1.0f - game.PipeSpeed, 0.0001f);
    }

    [Fact]
    public void RandomGapY_StaysWithinPlayableRange()
    {
        var game = new FlappyBirdGameComponent();
        var halfGap = game.PipeGap * 0.5f;
        var min = game.GroundTopY + halfGap + 0.25f;
        var max = game.CeilingY - halfGap - 0.25f;

        for (var i = 0; i < 200; i++)
        {
            var gap = FlappyBirdSystem.RandomGapY(game);
            gap.ShouldBeGreaterThanOrEqualTo(min);
            gap.ShouldBeLessThanOrEqualTo(max);
        }
    }
}
