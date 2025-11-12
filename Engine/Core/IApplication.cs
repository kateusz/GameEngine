namespace Engine.Core;

public interface IApplication
{
    void Run();
    void PushLayer(ILayer layer);
    void PushOverlay(ILayer overlay);

    /// <summary>
    /// Gets or sets the time scale for game simulation. Default is 1.0 (normal speed).
    /// </summary>
    float TimeScale { get; set; }

    /// <summary>
    /// Gets or sets whether the game simulation is paused.
    /// </summary>
    bool IsPaused { get; set; }

    /// <summary>
    /// Steps the simulation forward by a single frame when paused.
    /// </summary>
    void StepSingleFrame();
}