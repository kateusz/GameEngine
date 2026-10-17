using ECS;

namespace IslandDash;

public enum GamePhase
{
    Title,
    Playing,
    Paused,
    Dying,
    GameOver,
    LevelComplete
}

/// <summary>Runtime session state, added to the "Game" entity when the level is built (not saved in the scene).</summary>
public sealed class GameStateComponent : IGameComponent
{
    public GamePhase Phase { get; set; } = GamePhase.Title;
    public float PhaseTime { get; set; }

    public int Lives { get; set; }
    public int Crystals { get; set; }
    public int TotalCrystalsCollected { get; set; }
    public int CrystalsInLevel { get; set; }
    public int CratesBroken { get; set; }
    public int CratesInLevel { get; set; }
    public float LevelTime { get; set; }

    public Vector3 Checkpoint { get; set; }
    public float KillY { get; set; }

    /// <summary>Set by game flow; the level system tears the level down and rebuilds it at the start of the next frame.</summary>
    public bool RebuildRequested { get; set; } = true;
    /// <summary>Camera jumps to its target instead of smoothing (respawn, rebuild).</summary>
    public bool CameraSnap { get; set; } = true;
    /// <summary>World yaw the camera is looking along; player input is relative to it.</summary>
    public float CameraYaw { get; set; }

    public string Message { get; set; } = "";
    public string SubMessage { get; set; } = "";
    public float MessageTime { get; set; }

    public void Flash(string message, float seconds = 1.6f, string subMessage = "")
    {
        Message = message;
        SubMessage = subMessage;
        MessageTime = seconds;
    }

    public void SetPhase(GamePhase phase)
    {
        Phase = phase;
        PhaseTime = 0f;
    }

    public IComponent Clone() => (GameStateComponent)MemberwiseClone();
}
