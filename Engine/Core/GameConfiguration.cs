namespace Engine.Core;

public class GameConfiguration
{
    public string GameAssemblyPath { get; set; } = "GameAssembly.dll";
    public string StartupScenePath { get; set; } = "assets/scenes/game.scene";
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public string GameTitle { get; set; } = "My Game";
}
