using ECS;

namespace IslandDash;

/// <summary>Level/session settings on the scene's "Game" entity.</summary>
[SerializableComponent]
public class GameSettingsComponent : IGameComponent
{
    public string LevelPath { get; set; } = "levels/level1.lvl";
    public string ManifestPath { get; set; } = "config/assets.manifest";
    public int StartLives { get; set; } = 4;
    public int CrystalsPerExtraLife { get; set; } = 100;
    /// <summary>Falling below this height kills the player (a level's kill_plane piece overrides it).</summary>
    public float DefaultKillY { get; set; } = -14f;
    public bool ShowTitleScreen { get; set; } = true;

    public IComponent Clone() => (GameSettingsComponent)MemberwiseClone();
}
