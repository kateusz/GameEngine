using Engine.Core;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Benchmark;

internal sealed class HeadlessImGuiLayer : Ui.ImGui.IImGuiLayer
{
    public void OnAttach() { }
    public void OnDetach() { }
    public void OnUpdate(TimeSpan timeSpan) { }
    public void Draw() { }
    public void HandleInputEvent(InputEvent windowEvent) { }
    public void HandleWindowEvent(WindowEvent windowEvent) { }
    public void BeginFrame(TimeSpan elapsed) { }
    public void EndFrame() { }
}
