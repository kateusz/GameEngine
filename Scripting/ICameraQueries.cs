using System.Numerics;

namespace Scripting;

public interface ICameraQueries
{
    /// <summary>
    /// Maps a window-space point through the pointer surface and primary camera onto the Z=0 plane.
    /// Returns null when the cursor is outside the pointer surface, the surface is empty,
    /// there is no primary camera, or the ray misses.
    /// </summary>
    Vector2? ScreenToWorld2D(Vector2 windowPosition);
}
