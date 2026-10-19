namespace Hounds.Marches;

public static class Melee
{
    public static bool Aabb(float ax, float ay, float ahw, float ahh, float bx, float by, float bhw, float bhh) =>
        MathF.Abs(ax - bx) <= ahw + bhw && MathF.Abs(ay - by) <= ahh + bhh;

    // Block's Tick increments the frame before other actors strike, so the
    // observed window is frames 1..ParryFrames after that increment, plus frame 0
    // if a hit lands in the same pass. That stays inside 6–8 frames.
    public static bool InParry(int frame) => frame >= 0 && frame <= SliceRules.ParryFrames;

    public static bool InDodgeIFrames(string action, int frame) =>
        action == "dodge" && frame >= SliceRules.DodgeIFrameStart && frame <= SliceRules.DodgeIFrameEnd;

    public static void Hitbox(SliceRules.Attack atk, int facing, float x, float y,
        out float hx, out float hy, out float hw, out float hh)
    {
        hw = MathF.Max(0.35f, atk.Reach * 0.5f);
        hh = 0.48f;
        hx = x + facing * (0.28f + hw);
        hy = y + 0.1f;
    }

    public static void Verify()
    {
        if (!Aabb(0, 0, 1, 1, 1.5f, 0, 1, 1))
            throw new InvalidOperationException("Expected AABB overlap.");
        if (Aabb(0, 0, 0.2f, 0.2f, 2, 0, 0.2f, 0.2f))
            throw new InvalidOperationException("AABB false positive.");
        Hitbox(SliceRules.For("player", "light", false), 1, 0, 0, out var hx, out _, out _, out _);
        if (hx <= 0)
            throw new InvalidOperationException("Light hitbox should sit in front of a right-facing actor.");
        if (!InParry(0) || !InParry(SliceRules.ParryFrames) || InParry(SliceRules.ParryFrames + 1))
            throw new InvalidOperationException("Parry window must cover frames 0 through ParryFrames.");
        if (!InDodgeIFrames("dodge", SliceRules.DodgeIFrameStart) || !InDodgeIFrames("dodge", SliceRules.DodgeIFrameEnd)
            || InDodgeIFrames("dodge", SliceRules.DodgeIFrameStart - 1)
            || InDodgeIFrames("dodge", SliceRules.DodgeIFrameEnd + 1))
            throw new InvalidOperationException("Dodge i-frames must be frames 1 through 10.");
    }
}
