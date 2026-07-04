using ECS;
using Shouldly;
using TicTacToe;

namespace Engine.Tests;

public class TicTacToeSystemTests
{
    [Fact]
    public void ThreeInARow_SetsGameOverWithWinner()
    {
        var (system, board) = CreateSystemWithBoard();
        system.OnInit();

        foreach (var index in new[] { 0, 3, 1, 4, 2 })
        {
            board.PendingCellIndex = index;
            system.OnUpdate(TimeSpan.Zero);
        }

        board.GameOver.ShouldBeTrue();
        board.Winner.ShouldBe(BoardComponent.Cross);
    }

    [Fact]
    public void FullBoardWithNoWinner_SetsDraw()
    {
        var (system, board) = CreateSystemWithBoard();
        system.OnInit();

        // X O X / O O X / X X O
        foreach (var index in new[] { 0, 4, 1, 2, 6, 3, 5, 7, 8 })
        {
            board.PendingCellIndex = index;
            system.OnUpdate(TimeSpan.Zero);
        }

        board.GameOver.ShouldBeTrue();
        board.Winner.ShouldBe(BoardComponent.Draw);
    }

    private static (TicTacToeSystem System, BoardComponent Board) CreateSystemWithBoard()
    {
        var context = new Context();
        var entity = Entity.Create(1, "board");
        var board = new BoardComponent();
        entity.AddComponent(board);
        context.Register(entity);
        return (new TicTacToeSystem(context), board);
    }
}
