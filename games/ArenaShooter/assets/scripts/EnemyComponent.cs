using ECS;

namespace ArenaShooter.assets.scripts;

/// <summary>
/// Marks a pooled enemy entity. Position lives on its TransformComponent; this only
/// tracks whether the enemy is currently in play. Dead enemies are parked off-screen
/// and reused by the spawner.
/// </summary>
[SerializableComponent]
public class EnemyComponent : IGameComponent
{
    public bool Alive { get; set; }

    public IComponent Clone() => new EnemyComponent { Alive = Alive };
}
