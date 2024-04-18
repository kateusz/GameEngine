using Engine.Core;
using Engine.Events;
using Engine.Platform.SilkNet;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Engine.ImGuiNet;

public class ImGuiLayer : Layer
{
    private ImGuiController _controller;

    public ImGuiLayer(string name) : base(name)
    {
    }

    public override void OnImGuiRender()
    {
        ImGui.ShowDemoWindow();
        SubmitUI();
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
        var view = SilkNetContext.Window;
        var inputContext = SilkNetContext.InputContext;
        var gl = SilkNetContext.GL;

        _controller = new ImGuiController(gl, view, inputContext, OnConfigureIo);
    }

    private void OnConfigureIo()
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
    }

    public override void HandleEvent(Event @event)
    {
        if (@event is WindowResizeEvent windowResizeEvent)
        {
            //todo
            //_controller.WindowResized(windowResizeEvent.Width, windowResizeEvent.Height);
        }

        base.HandleEvent(@event);
    }

    private void SubmitUI()
    {
    }
}