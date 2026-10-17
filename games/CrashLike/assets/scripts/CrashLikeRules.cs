using System.Numerics;

namespace CrashLike.assets.scripts;

public enum DamageKind
{
    Ignored,
    Masked,
    Hurt,
    Dead
}

/// <summary>
/// Pure gameplay decisions. The system applies these to entities and audio.
/// </summary>
public static class CrashLikeRules
{
    public static bool Aabb(
        float ax, float ay, float ahw, float ahh,
        float bx, float by, float bhw, float bhh,
        float pad = 0f)
    {
        return MathF.Abs(ax - bx) <= ahw + bhw + pad
            && MathF.Abs(ay - by) <= ahh + bhh + pad;
    }

    public static bool IsStomp(float playerY, float playerHalfH, float vy, float otherY, float otherHalfH)
    {
        if (vy > -0.2f)
            return false;

        var feet = playerY - playerHalfH;
        var head = otherY + otherHalfH;
        return feet <= head + 0.14f && feet >= otherY - 0.08f;
    }

    public static bool Overlap3(Vector3 a, Vector3 ah, Vector3 b, Vector3 bh, float pad = 0f)
    {
        return MathF.Abs(a.X - b.X) <= ah.X + bh.X + pad
            && MathF.Abs(a.Y - b.Y) <= ah.Y + bh.Y + pad
            && MathF.Abs(a.Z - b.Z) <= ah.Z + bh.Z + pad;
    }

    public static float Strafe(bool left, bool right, float speed)
    {
        var value = 0f;
        if (left)
            value -= speed;
        if (right)
            value += speed;
        return value;
    }

    public static float TargetSpeed(bool depth, bool left, bool right, float moveSpeed)
    {
        if (depth)
            return moveSpeed;

        var speed = 0f;
        if (left)
            speed -= moveSpeed;
        if (right)
            speed += moveSpeed;
        return speed;
    }

    public static float MoveTowards(float current, float target, float maxDelta)
    {
        var delta = target - current;
        if (MathF.Abs(delta) <= maxDelta)
            return target;
        return current + MathF.Sign(delta) * maxDelta;
    }

    public static bool TryConsumeJump(CrashLikeGameComponent game, bool jumpPressed, bool grounded, float dt)
    {
        if (jumpPressed)
            game.JumpBuffer = game.JumpBufferTime;
        else if (game.JumpBuffer > 0f)
            game.JumpBuffer -= dt;

        if (grounded)
            game.CoyoteTimer = game.CoyoteTime;
        else if (game.CoyoteTimer > 0f)
            game.CoyoteTimer -= dt;

        if (game.JumpBuffer <= 0f || game.CoyoteTimer <= 0f)
            return false;

        game.JumpBuffer = 0f;
        game.CoyoteTimer = 0f;
        return true;
    }

    public static int AddWumpa(CrashLikeGameComponent game, int amount)
    {
        if (amount <= 0 || game.WumpaPerLife <= 0)
            return 0;

        game.Wumpa += amount;
        var gained = 0;
        while (game.Wumpa >= game.WumpaPerLife)
        {
            if (game.Lives >= game.MaxLives)
            {
                game.Wumpa = game.WumpaPerLife - 1;
                break;
            }

            game.Wumpa -= game.WumpaPerLife;
            game.Lives++;
            gained++;
        }

        return gained;
    }

    public static DamageKind TakeDamage(CrashLikeGameComponent game)
    {
        if (game.Phase != CrashLikeGameComponent.Playing || game.InvulnTimer > 0f || game.TeleportPending)
            return DamageKind.Ignored;

        if (game.Masks > 0)
        {
            game.Masks--;
            game.InvulnTimer = game.InvulnTime;
            return DamageKind.Masked;
        }

        return LoseLife(game);
    }

    /// <summary>Pits ignore the mask. Aku Aku does not save a fall.</summary>
    public static DamageKind Fall(CrashLikeGameComponent game)
    {
        if (game.Phase != CrashLikeGameComponent.Playing || game.TeleportPending)
            return DamageKind.Ignored;

        return LoseLife(game);
    }

    public static Vector2 StepEnemy(EnemyComponent enemy, float x, float playerX, float playerY)
    {
        var dir = enemy.Direction >= 0 ? 1 : -1;
        var speed = enemy.Speed;
        var chasing = enemy.Kind == EnemyComponent.Charger
            && MathF.Abs(playerX - x) <= enemy.ChargeRange
            && MathF.Abs(playerY - enemy.HomeY) <= 1.35f;

        if (chasing)
        {
            dir = playerX >= x ? 1 : -1;
            speed *= 2.15f;
        }
        else
        {
            if (x <= enemy.MinX)
                dir = 1;
            if (x >= enemy.MaxX)
                dir = -1;
        }

        enemy.Direction = dir;
        return new Vector2(dir * speed, 0f);
    }

    public static float ClampCamera(float desired, float halfWidth, float levelMin, float levelMax)
    {
        var min = levelMin + halfWidth;
        var max = levelMax - halfWidth;
        if (max < min)
            return (levelMin + levelMax) * 0.5f;
        return System.Math.Clamp(desired, min, max);
    }

    public static float Follow(float current, float target, float sharpness, float dt)
    {
        var t = 1f - MathF.Exp(-sharpness * dt);
        return current + (target - current) * t;
    }

    public static string FacingTexture(string path, int facing)
    {
        if (facing >= 0 || string.IsNullOrEmpty(path))
            return path;

        var dot = path.LastIndexOf('.');
        if (dot <= 0)
            return path;

        return path[..dot] + "_left" + path[dot..];
    }

    private static DamageKind LoseLife(CrashLikeGameComponent game)
    {
        game.Lives--;
        game.InvulnTimer = game.InvulnTime;
        if (game.Lives <= 0)
        {
            game.Lives = 0;
            game.Phase = CrashLikeGameComponent.GameOver;
            return DamageKind.Dead;
        }

        return DamageKind.Hurt;
    }
}
