using System.Numerics;

namespace Engine.Core.Window;

/// <summary>
/// Host-owned rectangle of the game view in window/logical pixels (same space as mouse position).
/// Editor Play publishes the ImGui viewport; Runtime publishes the client window.
/// </summary>
public interface IPointerSurface
{
    Vector2 Origin { get; }
    Vector2 Size { get; }

    void Set(Vector2 origin, Vector2 size);

    bool Contains(Vector2 windowPosition);
}
