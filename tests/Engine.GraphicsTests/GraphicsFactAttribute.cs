namespace Engine.GraphicsTests;

public sealed class GraphicsFactAttribute : FactAttribute
{
    private const string SkipMessage =
        "No headless GL stack available; see docs/specs/graphics-context-init-tests";

    public GraphicsFactAttribute()
    {
        if (IsCiEnvironment())
            return;

        if (!HeadlessGlProbe.IsAvailable)
            Skip = SkipMessage;
    }

    private static bool IsCiEnvironment()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")))
            return true;

        var ci = Environment.GetEnvironmentVariable("CI");
        return !string.IsNullOrEmpty(ci)
               && !string.Equals(ci, "false", StringComparison.OrdinalIgnoreCase);
    }
}
