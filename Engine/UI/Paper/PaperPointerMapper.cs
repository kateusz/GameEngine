using System.Numerics;
using Engine.Core.Window;

namespace Engine.UI.Paper;

/// <summary>
/// Maps window-space pointer coordinates to Paper framebuffer pixels using the active pointer surface.
/// </summary>
public static class PaperPointerMapper
{
  public readonly record struct MappedPointer(float X, float Y, bool IsInside);

  public static MappedPointer Map(Vector2 windowPosition, IPointerSurface surface, float contentScale)
  {
    if (surface.Size.X <= 0f || surface.Size.Y <= 0f || contentScale <= 0f)
      return new MappedPointer(0, 0, false);

    if (!surface.Contains(windowPosition))
      return new MappedPointer(0, 0, false);

    var local = windowPosition - surface.Origin;
    var fbX = local.X * contentScale;
    var fbY = local.Y * contentScale;
    return new MappedPointer(fbX, fbY, true);
  }
}
