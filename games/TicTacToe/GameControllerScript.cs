using Audio;
using ECS;
using Input;
using Scripting;

namespace TicTacToe;

public class GameControllerScript : ScriptableEntity
{
    public GameControllerScript(IComponentAccessor accessor, IAudio audio) : base(accessor, audio) { }

    public override void OnKeyPressed(KeyCodes key)
    {
        var board = GetComponent<BoardComponent>();

        if (key == KeyCodes.R)
        {
            Audio.PlayOneShot("assets/sounds/car-horn.wav");
            board.ResetRequested = true;
            return;
        }

        var index = KeyToIndex(key);
        if (index >= 0)
            board.PendingCellIndex = index;
    }

    private static int KeyToIndex(KeyCodes key) => key switch
    {
        KeyCodes.D1 => 0, KeyCodes.D2 => 1, KeyCodes.D3 => 2,
        KeyCodes.D4 => 3, KeyCodes.D5 => 4, KeyCodes.D6 => 5,
        KeyCodes.D7 => 6, KeyCodes.D8 => 7, KeyCodes.D9 => 8,
        _ => -1
    };
}