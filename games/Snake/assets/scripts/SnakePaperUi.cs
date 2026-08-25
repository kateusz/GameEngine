using ECS;
using Input;
using Prowl.PaperUI;
using Prowl.Scribe;
using Prowl.Vector;
using Scripting;
using UI.Paper;
using PaperGui = Prowl.PaperUI.Paper;

namespace Snake.assets.scripts;

[Register(typeof(IPaperUi))]
public sealed class SnakePaperUi(IContext context, IKeyboardInput keyboardInput) : IPaperUi
{
    private bool _paused;

    public bool BlocksGameplayInput => _paused || (FindGame()?.GameOver ?? false);

    public void Draw(PaperGui gui, FontFile font)
    {
        var game = FindGame();
        if (game == null)
            return;

        if (!game.GameOver && keyboardInput.WasKeyPressed(KeyCodes.Escape))
            _paused = !_paused;

        if (game.GameOver)
            _paused = true;

        using (gui.Box("SnakeHud")
                   .IsNotInteractable()
                   .PositionType(PositionType.SelfDirected)
                   .Left(12)
                   .Top(12)
                   .Padding(8)
                   .BackgroundColor(new Color(0, 0, 0, 0.45f))
                   .Rounded(6)
                   .Enter())
        {
            gui.Box("ScoreLabel")
                .IsNotInteractable()
                .Text($"Score: {game.Score}", font)
                .FontSize(35)
                .TextColor(Color.White);
        }

        if (!_paused && !game.GameOver)
        {
            game.Paused = false;
            return;
        }

        using (gui.Box("PauseOverlay")
                   .Width(gui.Stretch())
                   .Height(gui.Stretch())
                   .BackgroundColor(new Color(0, 0, 0, 0.55f))
                   .OnClick(_ => { })
                   .Enter())
        {
            using (gui.Column("PauseMenu")
                       .Width(260)
                       .Height(gui.Auto)
                       .Margin(gui.Stretch(), gui.Stretch(), gui.Stretch(), gui.Stretch())
                       .Padding(16)
                       .BackgroundColor(new Color(30 / 255f, 30 / 255f, 30 / 255f, 1f))
                       .Rounded(10)
                       .Enter())
            {
                gui.Box("PauseTitle")
                    .IsNotInteractable()
                    .Height(36)
                    .Text(game.GameOver ? "Game Over" : "Paused", font)
                    .FontSize(28)
                    .TextColor(Color.White)
                    .Alignment(Prowl.PaperUI.TextAlignment.MiddleCenter);

                gui.Box("ResumeButton")
                    .Height(40)
                    .BackgroundColor(new Color(46 / 255f, 125 / 255f, 50 / 255f, 1f))
                    .Rounded(6)
                    .OnClick(_ =>
                    {
                        if (game.GameOver)
                            SnakeSystem.ResetGame(game);
                        _paused = false;
                    })
                    .Text(game.GameOver ? "Restart" : "Resume", font)
                    .FontSize(20)
                    .TextColor(Color.White)
                    .Alignment(Prowl.PaperUI.TextAlignment.MiddleCenter);
            }
        }

        game.Paused = _paused || game.GameOver;
    }

    private SnakeGameComponent? FindGame()
    {
        foreach (var (_, game) in context.View<SnakeGameComponent>())
            return game;
        return null;
    }
}