using Editor.Features.Shell;
using Editor.Input;
using Editor.Panels;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events.Input;
using Engine.Events.Window;
using ImGuiNET;

namespace Editor;

public class EditorLayer(
    EditorLifecycle lifecycle,
    EditorDockspace dockspace,
    EditorInputHandler inputHandler,
    PerformanceMonitorPanel performanceMonitor) : ILayer
{
    public void OnAttach(IInputSystem inputSystem) => lifecycle.Attach(inputSystem);

    public void OnDetach() => lifecycle.Detach();

    public void OnUpdate(TimeSpan timeSpan) => performanceMonitor.Update(timeSpan);

    public void Draw() => dockspace.Draw(TimeSpan.FromSeconds(ImGui.GetIO().DeltaTime));

    public void HandleInputEvent(InputEvent windowEvent) => inputHandler.Handle(windowEvent);

    public void HandleWindowEvent(WindowEvent windowEvent) { }
}
