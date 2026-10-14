using ECS;

namespace Platformer.assets.scripts;

/// <summary>One cell in the on-screen tile pool. The system maps it onto the visible level.</summary>
[SerializableComponent]
public class LevelTileComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new LevelTileComponent { Index = Index };
}
