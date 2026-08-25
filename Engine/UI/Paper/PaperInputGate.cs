using Engine.Events.Input;

namespace Engine.UI.Paper;

public sealed class PaperInputGate
{
  public bool BlocksMouse { get; private set; }
  public bool BlocksKeyboard { get; private set; }

  internal void BeginFrame()
  {
    BlocksMouse = false;
    BlocksKeyboard = false;
  }

  internal void SetCapture(bool blocksMouse, bool blocksKeyboard)
  {
    BlocksMouse = blocksMouse;
    BlocksKeyboard = blocksKeyboard;
  }

  public static bool Blocks(InputEvent inputEvent, PaperInputGate gate) =>
    inputEvent switch
    {
      KeyPressedEvent or KeyReleasedEvent => gate.BlocksKeyboard,
      MouseButtonPressedEvent or MouseButtonReleasedEvent or MouseMovedEvent or MouseScrolledEvent =>
        gate.BlocksMouse,
      _ => false
    };
}
