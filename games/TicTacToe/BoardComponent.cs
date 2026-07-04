using ECS;

namespace TicTacToe;

[SerializableComponent]
public class BoardComponent : IGameComponent
{
    public const int Empty = 0;
    public const int Cross = 1;
    public const int Circle = 2;
    public const int Draw = 3;
    public const int NoWinner = 0;

    public int[] Cells { get; set; } = new int[9];
    public int CurrentPlayer { get; set; } = Cross;
    public bool GameOver { get; set; }
    public int Winner { get; set; }

    public void Reset()
    {
        Array.Fill(Cells, Empty);
        CurrentPlayer = Cross;
        GameOver = false;
        Winner = NoWinner;
    }

    public IComponent Clone() => new BoardComponent
    {
        Cells = (int[])Cells.Clone(),
        CurrentPlayer = CurrentPlayer,
        GameOver = GameOver,
        Winner = Winner
    };
}
