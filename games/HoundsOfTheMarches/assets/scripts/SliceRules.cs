namespace Hounds.Marches;

/// <summary>
/// Combat numbers for the vertical slice. Frames are at 60 Hz.
/// Enemy windups stay at least 21 frames (0.35 s). Parry is 7 frames.
/// Dodge i-frames are frames 1 through 10 inclusive (10 frames).
/// </summary>
public static class SliceRules
{
    public const float Dt = 1f / 60f;
    public const int ParryFrames = 7;
    public const int DodgeIFrameStart = 1;
    public const int DodgeIFrameEnd = 10;
    public const int DodgeFrames = 18;
    public const int StaggerFrames = 28;
    public const int MinEnemyWindup = 21;

    public const float PlayerHp = 120f;
    public const float PlayerPoise = 40f;
    public const float PlayerStamina = 100f;
    public const float RunSpeed = 6.4f;
    public const float JumpSpeed = 8.4f;
    public const float DodgeSpeed = 11.5f;

    public readonly record struct Attack(
        string Name, int Windup, int Active, int Recover,
        float Damage, float Poise, float Stamina, float Reach, float Knock);

    public static Attack For(string kind, string action, bool phase2)
    {
        var atk = (kind, action) switch
        {
            ("player", "heavy") => new Attack("heavy", 22, 8, 18, 34f, 28f, 28f, 1.15f, 7.5f),
            ("player", "light") => new Attack("light", 8, 6, 12, 16f, 12f, 14f, 0.95f, 4.2f),
            ("player", "dodge") => new Attack("dodge", 0, 0, DodgeFrames, 0, 0, 26f, 0, 0),
            ("boss", "thrust") => new Attack("thrust", 27, 8, 16, 22f, 18f, 0, 1.85f, 6.5f),
            ("boss", "leap") => new Attack("leap", 33, 10, 18, 28f, 24f, 0, 1.35f, 9f),
            ("boss", "slash") => new Attack("slash", 24, 8, 16, 20f, 16f, 0, 1.45f, 6f),
            ("halberdier", _) => new Attack("light", 36, 10, 20, 24f, 22f, 0, 1.7f, 7f),
            _ => new Attack("light", 24, 8, 14, 14f, 12f, 0, 1.15f, 4.5f)
        };

        if (phase2 && kind == "boss" && atk.Windup > 0)
            atk = atk with { Windup = System.Math.Max(MinEnemyWindup, atk.Windup * 3 / 4) };
        return atk;
    }

    public static (float HalfW, float HalfH) Body(string kind) => kind switch
    {
        "boss" => (0.42f, 0.95f),
        "halberdier" => (0.38f, 0.82f),
        "swordsman" => (0.30f, 0.74f),
        _ => (0.32f, 0.78f)
    };

    public static (float W, float H) SpriteSize(string pack, string kind)
    {
        if (pack == "external")
            return kind switch
            {
                "boss" => (1.3f, 2.6f),
                "halberdier" => (1.1f, 2.15f),
                _ => (1.0f, 2.0f)
            };

        return kind switch
        {
            "boss" => (2.2f, 2.45f),
            "halberdier" => (1.9f, 2.1f),
            "swordsman" => (1.5f, 1.85f),
            _ => (1.6f, 1.9f)
        };
    }

    public static int StingersFor(string kind) => kind switch
    {
        "boss" => 40,
        "halberdier" => 10,
        "swordsman" => 6,
        _ => 0
    };

    public static string Clip(string kind, string action, int frame, bool phase2, bool moving, bool grounded)
    {
        if (action is "dead" or "stagger")
            return "hurt";
        if (action == "dodge")
            return "dodge";
        if (action == "block")
            return "block";

        if (action is "light" or "heavy" or "slash" or "thrust" or "leap")
        {
            var atk = For(kind, action, phase2);
            var wind = frame < atk.Windup;
            var active = frame < atk.Windup + atk.Active;
            if (kind == "boss")
                return action switch
                {
                    "thrust" => wind ? "thrustWindup" : active ? "thrust" : "recover",
                    "leap" => "leap",
                    _ => wind ? "slashWindup" : active ? "slash" : "recover"
                };
            if (kind == "player")
                return action == "heavy"
                    ? wind ? "heavyWindup" : active ? "heavy" : "recover"
                    : wind ? "lightWindup" : active ? "light" : "recover";
            return wind ? "windup" : active ? "active" : "recover";
        }

        if (!grounded)
            return "air";
        if (moving)
            return "run";
        return phase2 && kind == "boss" ? "phase2" : "idle";
    }

    public static void Verify()
    {
        if (For("player", "heavy", false).Windup < MinEnemyWindup)
            throw new InvalidOperationException("Player heavy windup is under 0.35s.");

        foreach (var kind in new[] { "swordsman", "halberdier" })
        {
            var w = For(kind, "light", false).Windup;
            if (w < MinEnemyWindup)
                throw new InvalidOperationException($"{kind} windup {w} < {MinEnemyWindup}.");
        }

        foreach (var action in new[] { "slash", "thrust", "leap" })
        {
            if (For("boss", action, false).Windup < MinEnemyWindup)
                throw new InvalidOperationException($"Boss {action} windup is under 0.35s.");
            if (For("boss", action, true).Windup < MinEnemyWindup)
                throw new InvalidOperationException($"Boss phase-2 {action} windup is under 0.35s.");
        }
    }
}
