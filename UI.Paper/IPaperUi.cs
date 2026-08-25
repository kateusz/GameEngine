using Prowl.Scribe;
using PaperGui = Prowl.PaperUI.Paper;

namespace UI.Paper;

/// <summary>
/// Game-provided runtime overlay UI drawn each Paper frame by the host.
/// </summary>
public interface IPaperUi
{
  void Draw(PaperGui paper, FontFile defaultFont);

  /// <summary>
  /// When true, keyboard input is blocked from gameplay scripts for this frame (e.g. pause menu).
  /// </summary>
  bool BlocksGameplayInput => false;
}
