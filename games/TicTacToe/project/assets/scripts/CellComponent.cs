using ECS;

namespace TicTacToe.project.assets.scripts;

[SerializableComponent]
public class CellComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new CellComponent { Index = Index };
}