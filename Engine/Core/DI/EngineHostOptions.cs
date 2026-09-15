using Engine.Core.Window;

namespace Engine.Core.DI;

public sealed record EngineHostOptions(string WindowTitle, int WindowWidth, int WindowHeight)
{
    public bool Maximized { get; init; }
    public bool Fullscreen { get; init; }
    public int TargetFrameRate { get; init; }

    public static EngineHostOptions EditorDefaults => new(
        "MulEngine",
        (int)DisplayConfig.DefaultWindowWidth,
        (int)DisplayConfig.DefaultWindowHeight)
    {
        Maximized = true
    };
}
