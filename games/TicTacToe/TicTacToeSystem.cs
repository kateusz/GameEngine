using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents.Rendering;
using Scripting;

namespace TicTacToe;

[Register(typeof(IGameSystem))]
public class TicTacToeSystem(IContext context) : IGameSystem
{
    public int Priority => 115;

    public void OnInit() { }

    public void OnUpdate(TimeSpan deltaTime)
    {
        var board = FindBoard();
        if (board == null)
            return;

        if (board.ResetRequested)
        {
            board.Reset();
            Console.WriteLine("Board reset. X's turn.");
        }

        if (board.PendingCellIndex >= 0)
        {
            TryPlace(board, board.PendingCellIndex);
            board.PendingCellIndex = -1;
        }

        SyncCellVisuals(board);
    }

    public void OnShutdown() { }

    private BoardComponent? FindBoard()
    {
        foreach (var (entity, board) in context.View<BoardComponent>())
            return board;
        return null;
    }

    private static void TryPlace(BoardComponent board, int index)
    {
        if (board.GameOver || board.Cells[index] != BoardComponent.Empty)
            return;

        var player = board.CurrentPlayer;
        board.Cells[index] = player;

        if (CheckWin(board, player))
        {
            board.GameOver = true;
            Console.WriteLine($"{PlayerName(player)} wins!");
            return;
        }

        if (IsBoardFull(board))
        {
            board.GameOver = true;
            Console.WriteLine("Draw!");
            return;
        }

        board.CurrentPlayer = player == BoardComponent.Cross
            ? BoardComponent.Circle
            : BoardComponent.Cross;

        Console.WriteLine($"{PlayerName(board.CurrentPlayer)}'s turn");
    }

    private void SyncCellVisuals(BoardComponent board)
    {
        foreach (var (entity, cell) in context.View<CellComponent>())
        {
            if (!entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
                continue;

            sprite.Color = board.Cells[cell.Index] switch
            {
                BoardComponent.Cross => new Vector4(0.9f, 0.2f, 0.2f, 1f),
                BoardComponent.Circle => new Vector4(0.2f, 0.4f, 0.9f, 1f),
                _ => new Vector4(0.25f, 0.25f, 0.25f, 1f)
            };
        }
    }

    // CheckWin, IsBoardFull, PlayerName — same as before
    private static bool CheckWin(BoardComponent board, int player) { /* ... */ return false; }
    private static bool IsBoardFull(BoardComponent board) { /* ... */ return false; }
    private static string PlayerName(int player) => player switch
    {
        BoardComponent.Cross => "X",
        BoardComponent.Circle => "O",
        _ => "?"
    };
}