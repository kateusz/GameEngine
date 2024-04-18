using Engine.Core.Input;
using Silk.NET.Input;

namespace Engine.Platform.SilkNet.Input;

public class SilkNetKeyboardState : IKeyboardState
{
    public bool IsKeyPressed(KeyCodes keycode)
    {
        return SilkNetGameWindow.Keyboard.IsKeyPressed((Key)((int)keycode));
    }
}