using CrashLike.assets.scripts;
using Shouldly;

namespace Engine.Tests;

public class CrashLikeRulesTests
{
    [Fact]
    public void AddWumpa_EveryTen_GrantsALifeAndWraps()
    {
        var game = new CrashLikeGameComponent { Lives = 3, MaxLives = 5, Wumpa = 8, WumpaPerLife = 10 };

        var gained = CrashLikeRules.AddWumpa(game, 5);

        gained.ShouldBe(1);
        game.Lives.ShouldBe(4);
        game.Wumpa.ShouldBe(3);
    }

    [Fact]
    public void AddWumpa_AtMaxLives_StopsAtNine()
    {
        var game = new CrashLikeGameComponent { Lives = 5, MaxLives = 5, Wumpa = 8, WumpaPerLife = 10 };

        CrashLikeRules.AddWumpa(game, 5);

        game.Lives.ShouldBe(5);
        game.Wumpa.ShouldBe(9);
    }

    [Fact]
    public void TakeDamage_ConsumesMaskBeforeALife()
    {
        var game = new CrashLikeGameComponent { Lives = 3, Masks = 1, InvulnTime = 1f };

        var kind = CrashLikeRules.TakeDamage(game);

        kind.ShouldBe(DamageKind.Masked);
        game.Masks.ShouldBe(0);
        game.Lives.ShouldBe(3);
        game.InvulnTimer.ShouldBe(1f);
    }

    [Fact]
    public void TakeDamage_DuringInvulnerability_IsIgnored()
    {
        var game = new CrashLikeGameComponent { Lives = 3, InvulnTimer = 0.2f };

        CrashLikeRules.TakeDamage(game).ShouldBe(DamageKind.Ignored);
        game.Lives.ShouldBe(3);
    }

    [Fact]
    public void Fall_IgnoresMaskAndRemovesALife()
    {
        var game = new CrashLikeGameComponent { Lives = 2, Masks = 1, InvulnTime = 1f };

        var kind = CrashLikeRules.Fall(game);

        kind.ShouldBe(DamageKind.Hurt);
        game.Lives.ShouldBe(1);
        game.Masks.ShouldBe(1);
    }

    [Fact]
    public void Fall_OnLastLife_EndsTheRun()
    {
        var game = new CrashLikeGameComponent { Lives = 1 };

        CrashLikeRules.Fall(game).ShouldBe(DamageKind.Dead);
        game.Phase.ShouldBe(CrashLikeGameComponent.GameOver);
        game.Lives.ShouldBe(0);
    }

    [Fact]
    public void TryConsumeJump_GroundedPress_ConsumesBuffer()
    {
        var game = new CrashLikeGameComponent { JumpBufferTime = 0.12f, CoyoteTime = 0.12f };

        var jumped = CrashLikeRules.TryConsumeJump(game, jumpPressed: true, grounded: true, dt: 0.016f);

        jumped.ShouldBeTrue();
        game.JumpBuffer.ShouldBe(0f);
        game.CoyoteTimer.ShouldBe(0f);
    }

    [Fact]
    public void TryConsumeJump_InAirWithoutCoyote_DoesNotJump()
    {
        var game = new CrashLikeGameComponent { JumpBufferTime = 0.12f, CoyoteTime = 0.12f };

        var jumped = CrashLikeRules.TryConsumeJump(game, jumpPressed: true, grounded: false, dt: 0.016f);

        jumped.ShouldBeFalse();
        game.JumpBuffer.ShouldBeGreaterThan(0f);
    }

    [Fact]
    public void TargetSpeed_Depth_AlwaysRunsForward()
    {
        CrashLikeRules.TargetSpeed(depth: true, left: true, right: false, moveSpeed: 6.5f).ShouldBe(6.5f);
    }

    [Fact]
    public void TargetSpeed_Side_UsesKeys()
    {
        CrashLikeRules.TargetSpeed(depth: false, left: true, right: false, moveSpeed: 6.5f).ShouldBe(-6.5f);
        CrashLikeRules.TargetSpeed(depth: false, left: true, right: true, moveSpeed: 6.5f).ShouldBe(0f);
    }

    [Fact]
    public void IsStomp_RequiresFallingOntoTheHead()
    {
        CrashLikeRules.IsStomp(playerY: 1.2f, playerHalfH: 0.5f, vy: -3f, otherY: 0.5f, otherHalfH: 0.5f)
            .ShouldBeTrue();
        CrashLikeRules.IsStomp(playerY: 1.2f, playerHalfH: 0.5f, vy: 2f, otherY: 0.5f, otherHalfH: 0.5f)
            .ShouldBeFalse();
    }

    [Fact]
    public void StepEnemy_ReversesAtPatrolEnds()
    {
        var enemy = new EnemyComponent
        {
            Kind = EnemyComponent.Patrol,
            MinX = 0f,
            MaxX = 4f,
            Speed = 2f,
            Direction = 1
        };

        var velocity = CrashLikeRules.StepEnemy(enemy, x: 4.2f, playerX: 0f, playerY: 0f);

        enemy.Direction.ShouldBe(-1);
        velocity.X.ShouldBe(-2f);
    }

    [Fact]
    public void StepEnemy_Charger_ClosesOnThePlayer()
    {
        var enemy = new EnemyComponent
        {
            Kind = EnemyComponent.Charger,
            HomeY = 1f,
            MinX = 0f,
            MaxX = 10f,
            Speed = 2f,
            Direction = 1,
            ChargeRange = 5f
        };

        var velocity = CrashLikeRules.StepEnemy(enemy, x: 6f, playerX: 3f, playerY: 1f);

        enemy.Direction.ShouldBe(-1);
        velocity.X.ShouldBeLessThan(-2f);
    }

    [Fact]
    public void ClampCamera_KeepsTheLevelEdgesOnScreen()
    {
        CrashLikeRules.ClampCamera(desired: 0f, halfWidth: 8f, levelMin: 0f, levelMax: 94f).ShouldBe(8f);
        CrashLikeRules.ClampCamera(desired: 100f, halfWidth: 8f, levelMin: 0f, levelMax: 94f).ShouldBe(86f);
    }

    [Fact]
    public void FacingTexture_InsertsLeftSuffix()
    {
        CrashLikeRules.FacingTexture("textures/enemies/slime.png", facing: -1)
            .ShouldBe("textures/enemies/slime_left.png");
        CrashLikeRules.FacingTexture("textures/enemies/slime.png", facing: 1)
            .ShouldBe("textures/enemies/slime.png");
    }
}
