using ECS;

namespace TicTacToe;

[SerializableComponent]
public class BoardComponent : IGameComponent
{
    public const int Empty = 0;
    public const int Cross = 1;
    public const int Circle = 2;

    public int[] Cells { get; set; } = new int[9];
    public int CurrentPlayer { get; set; } = Cross;
    public bool GameOver { get; set; }

    // Input queue — script writes, system consumes
    public int PendingCellIndex { get; set; } = -1;
    public bool ResetRequested { get; set; }

    public void Reset()
    {
        Array.Fill(Cells, Empty);
        CurrentPlayer = Cross;
        GameOver = false;
        PendingCellIndex = -1;
        ResetRequested = false;
    }

    public IComponent Clone() => new BoardComponent
    {
        Cells = (int[])Cells.Clone(),
        CurrentPlayer = CurrentPlayer,
        GameOver = GameOver,
        PendingCellIndex = PendingCellIndex,
        ResetRequested = ResetRequested
    };
}