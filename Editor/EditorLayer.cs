using Editor.Features.Shell;
using Editor.Input;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;

namespace Editor;

public class EditorLayer(
    EditorLifecycle lifecycle,
    EditorDockspace dockspace,
    EditorInputHandler inputHandler) : ILayer
{
    private TimeSpan _delta;

    public void OnAttach(IInputSystem inputSystem) => lifecycle.Attach(inputSystem);

    public void OnDetach() => lifecycle.Detach();

    public void OnUpdate(TimeSpan timeSpan) => _delta = timeSpan;

    public void Draw() => dockspace.Draw(_delta);

    public void HandleInputEvent(InputEvent windowEvent) => inputHandler.Handle(windowEvent);
}
