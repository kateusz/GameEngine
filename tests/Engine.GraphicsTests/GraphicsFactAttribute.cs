using Engine.Platform.SilkNet;
using Silk.NET.Maths;

namespace Engine.GraphicsTests;

public sealed class GraphicsFactAttribute : FactAttribute
{
    private static readonly Lazy<bool> GlAvailable = new(ProbeGl);

    private const string SkipMessage =
        "No headless GL stack available; see docs/specs/graphics-context-init-tests";

    public GraphicsFactAttribute()
    {
        if (IsCiEnvironment())
            return;

        if (!GlAvailable.Value)
            Skip = SkipMessage;
    }

    private static bool ProbeGl()
    {
        try
        {
            var window = HeadlessWindow.Create("GL probe", new Vector2D<int>(1, 1));
            var context = new SilkNetGraphicsContext(window);
            context.Create();
            context.Dispose();
            window.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
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
