namespace Runtime;

public class GameConfiguration
{
    public string StartupScenePath { get; set; } = "assets/scenes/Scene.scene";
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; } = false;
    public string GameTitle { get; set; } = "My Game";
    public int TargetFrameRate { get; set; } = 60;
}
