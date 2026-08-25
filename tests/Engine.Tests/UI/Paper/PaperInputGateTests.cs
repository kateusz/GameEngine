using Engine.UI.Paper;
using Shouldly;

namespace Engine.Tests.UI.Paper;

public class PaperInputGateTests
{
  [Fact]
  public void BeginFrame_ClearsCaptureFlags()
  {
    var gate = new PaperInputGate();
    gate.SetCapture(true, true);
    gate.BeginFrame();

    gate.BlocksMouse.ShouldBeFalse();
    gate.BlocksKeyboard.ShouldBeFalse();
  }

  [Fact]
  public void SetCapture_StoresMouseAndKeyboardBlocks()
  {
    var gate = new PaperInputGate();
    gate.SetCapture(blocksMouse: true, blocksKeyboard: false);

    gate.BlocksMouse.ShouldBeTrue();
    gate.BlocksKeyboard.ShouldBeFalse();
  }
}
