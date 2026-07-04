namespace TicTacToe;

public static class GameState
{
    public const int Empty = 0;
    public const int Cross = 1;
    public const int Circle = 2;

    private static readonly int[][] WinLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8],
        [0, 3, 6], [1, 4, 7], [2, 5, 8],
        [0, 4, 8], [2, 4, 6]
    ];

    public static int CurrentPlayer = Cross;
    public static readonly int[] Board = new int[9];
    public static bool GameOver;

    public static void Reset()
    {
        Array.Fill(Board, Empty);
        CurrentPlayer = Cross;
        GameOver = false;
    }

    public static bool CheckWin(int player)
    {
        foreach (var line in WinLines)
        {
            if (Board[line[0]] == player &&
                Board[line[1]] == player &&
                Board[line[2]] == player)
                return true;
        }

        return false;
    }

    public static bool IsBoardFull()
    {
        foreach (var cell in Board)
        {
            if (cell == Empty)
                return false;
        }

        return true;
    }

    public static string PlayerName(int player) => player switch
    {
        Cross => "X",
        Circle => "O",
        _ => "?"
    };
}
