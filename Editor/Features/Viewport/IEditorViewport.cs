using System.Numerics;
using ECS;
using Engine.Events.Input;

namespace Editor.Features.Viewport;

public interface IEditorViewport : IDisposable
{
    EditorCamera Camera { get; }
    Entity? HoveredEntity { get; }
    bool IsHovered { get; }
    /// <summary>Screen rect of the Viewport dock window (for center-column overlays).</summary>
    Vector2 WindowPos { get; }
    Vector2 WindowSize { get; }
    bool HasWindowRect { get; }
    void Initialize();
    void LayoutAndRender(TimeSpan deltaTime);
    void DrawOverlays();
    void HandleWindowInput(InputEvent windowEvent);
}
