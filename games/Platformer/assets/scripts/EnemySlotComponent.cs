using ECS;

namespace Platformer.assets.scripts;

/// <summary>Pooled enemy sprite. <see cref="Index"/> matches <c>PlatformerGameComponent</c> enemy arrays.</summary>
[SerializableComponent]
public class EnemySlotComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new EnemySlotComponent { Index = Index };
}
