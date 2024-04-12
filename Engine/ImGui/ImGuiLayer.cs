using Engine.Core;
using Engine.Events;

namespace Engine.ImGui;

public class ImGuiLayer : Layer
{
    private readonly ImGuiController _controller;
    
    public ImGuiLayer(string name) : base(name)
    {
        _controller = new ImGuiController();
    }

    public override void OnImGuiRender()
    {
        ImGuiNET.ImGui.DockSpaceOverViewport();
        ImGuiNET.ImGui.ShowDemoWindow();
        //SubmitUI();
    }

    public void Begin(TimeSpan timeSpan)
    {
        _controller.Update((float)timeSpan.TotalSeconds);
    }

    public void End()
    {
        _controller.Render();
    }

    public override void OnAttach()
    {
        
    }

    public override void HandleEvent(Event @event)
    {
        if (@event is WindowResizeEvent windowResizeEvent)
        {
            _controller.WindowResized(windowResizeEvent.Width, windowResizeEvent.Height);
        }
        base.HandleEvent(@event);
    }
}