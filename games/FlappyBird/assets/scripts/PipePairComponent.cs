using ECS;

namespace FlappyBird.project.assets.scripts;

[SerializableComponent]
public class PipePairComponent : IGameComponent
{
    public int Index { get; set; }
    public bool IsTop { get; set; }

    public IComponent Clone() => new PipePairComponent { Index = Index, IsTop = IsTop };
}
