using ECS;
using Engine.Events.Input;
using Engine.Scene.Cameras;

namespace Editor.Features.Viewport;

public interface IEditorViewport : IDisposable
{
    EditorCamera Camera { get; }
    Entity? HoveredEntity { get; }
    bool IsHovered { get; }
    void Initialize();
    void LayoutAndRender(TimeSpan deltaTime);
    void DrawOverlays();
    void HandleWindowInput(InputEvent windowEvent);
}
