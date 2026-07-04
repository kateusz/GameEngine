using Audio;
using ECS;
using Input;
using NSubstitute;
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
            TicTacToeSystem.TryPlace(board, index);
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

        foreach (var index in new[] { 0, 4, 1, 2, 6, 3, 5, 7, 8 })
        {
            TicTacToeSystem.TryPlace(board, index);
            system.OnUpdate(TimeSpan.Zero);
        }

        board.GameOver.ShouldBeTrue();
        board.Winner.ShouldBe(BoardComponent.Draw);
    }

    [Fact]
    public void OnUpdate_WasKeyPressedPlacesMark()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        keyboard.WasKeyPressed(KeyCodes.D5).Returns(true);

        var (system, board) = CreateSystemWithBoard(keyboard);
        system.OnUpdate(TimeSpan.Zero);

        board.Cells[4].ShouldBe(BoardComponent.Cross);
    }

    [Fact]
    public void OnUpdate_ResetKeyClearsBoard()
    {
        var keyboard = Substitute.For<IKeyboardInput>();
        keyboard.WasKeyPressed(KeyCodes.D1).Returns(true);
        var (system, board) = CreateSystemWithBoard(keyboard);
        system.OnUpdate(TimeSpan.Zero);
        board.Cells[0].ShouldBe(BoardComponent.Cross);

        keyboard.WasKeyPressed(KeyCodes.D1).Returns(false);
        keyboard.WasKeyPressed(KeyCodes.R).Returns(true);
        system.OnUpdate(TimeSpan.Zero);

        board.Cells[0].ShouldBe(BoardComponent.Empty);
        board.CurrentPlayer.ShouldBe(BoardComponent.Cross);
    }

    private static (TicTacToeSystem System, BoardComponent Board) CreateSystemWithBoard(IKeyboardInput? keyboard = null)
    {
        var context = new Context();
        var entity = Entity.Create(1, "board");
        var board = new BoardComponent();
        entity.AddComponent(board);
        context.Register(entity);
        keyboard ??= Substitute.For<IKeyboardInput>();
        var audio = Substitute.For<IAudio>();
        return (new TicTacToeSystem(context, keyboard, audio), board);
    }
}
