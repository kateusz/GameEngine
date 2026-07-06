using System.Numerics;
using ECS;
using ECS.Systems;
using SceneComponents.Rendering;
using Scripting;

namespace TicTacToe.project.assets.scripts;

[Register(typeof(IGameSystem))]
public class TicTacToeSystem(IContext context) : IGameSystem
{
    private static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

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
        SyncGameOverBanner(board);
    }

    public void OnShutdown() { }

    private BoardComponent? FindBoard()
    {
        foreach (var (_, board) in context.View<BoardComponent>())
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
            board.Winner = player;
            Console.WriteLine($"{PlayerName(player)} wins!");
            return;
        }

        if (IsBoardFull(board))
        {
            board.GameOver = true;
            board.Winner = BoardComponent.Draw;
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

            switch (board.Cells[cell.Index])
            {
                case BoardComponent.Cross:
                    sprite.TexturePath = "textures/X.png";
                    sprite.Color = Vector4.One;
                    break;
                case BoardComponent.Circle:
                    sprite.TexturePath = "textures/O.png";
                    sprite.Color = Vector4.One;
                    break;
                default:
                    sprite.TexturePath = null;
                    sprite.Color =  Vector4.One;
                    break;
            }
        }
    }

    private void SyncGameOverBanner(BoardComponent board)
    {
        SetBanner("GameOverBanner", board.GameOver
            ? board.Winner switch
            {
                BoardComponent.Cross => "textures/x_wins.png",
                BoardComponent.Circle => "textures/o_wins.png",
                BoardComponent.Draw => "textures/draw.png",
                _ => null
            }
            : null);

        SetBanner("ResetHint", board.GameOver ? "textures/press_r.png" : null);
    }

    private void SetBanner(string entityName, string? texturePath)
    {
        var entity = context.GetByName(entityName);
        if (entity == null || !entity.TryGetComponent<SpriteRendererComponent>(out var sprite))
            return;

        sprite.TexturePath = texturePath;
        sprite.Color = Vector4.One;
    }

    private static bool CheckWin(BoardComponent board, int player)
    {
        foreach (var line in WinLines)
        {
            if (board.Cells[line[0]] == player &&
                board.Cells[line[1]] == player &&
                board.Cells[line[2]] == player)
                return true;
        }

        return false;
    }

    private static bool IsBoardFull(BoardComponent board)
    {
        foreach (var cell in board.Cells)
        {
            if (cell == BoardComponent.Empty)
                return false;
        }

        return true;
    }

    private static string PlayerName(int player) => player switch
    {
        BoardComponent.Cross => "X",
        BoardComponent.Circle => "O",
        _ => "?"
    };
}
