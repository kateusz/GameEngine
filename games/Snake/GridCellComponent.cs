using ECS;

namespace Snake;

[SerializableComponent]
public class GridCellComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new GridCellComponent { Index = Index };
}
