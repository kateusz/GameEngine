using ECS;

namespace Snake.project.assets.scripts;

[SerializableComponent]
public class GridCellComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new GridCellComponent { Index = Index };
}
