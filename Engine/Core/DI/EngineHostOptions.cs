namespace Engine.Core.DI;

public sealed record EngineHostOptions(string WindowTitle, int WindowWidth, int WindowHeight)
{
    public static EngineHostOptions EditorDefaults => new(
        "MulEngine",
        (int)DisplayConfig.DefaultWindowWidth,
        (int)DisplayConfig.DefaultWindowHeight);
}
