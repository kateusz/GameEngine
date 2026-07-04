using ECS;

namespace TicTacToe;

[SerializableComponent]
public class CellComponent : IGameComponent
{
    public int Index { get; set; }

    public IComponent Clone() => new CellComponent { Index = Index };
}