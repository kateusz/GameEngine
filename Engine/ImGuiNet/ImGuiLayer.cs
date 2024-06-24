using Engine.Core;
using Engine.Events;
using Engine.Platform.SilkNet;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Engine.ImGuiNet;

public class ImGuiLayer : Layer
{
    private ImGuiController _controller;

    public ImGuiLayer(string name) : base(name)
    {
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
    }

    public override void OnImGuiRender()
    {
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

    public override void HandleEvent(Event @event)
    {
        if (@event is WindowCloseEvent)
        {
            _controller.Dispose();
        }

        if (BlockEvents)
        {
            var io = ImGui.GetIO();
            @event.IsHandled |= @event.IsInCategory(EventCategory.EventCategoryMouse) & io.WantCaptureMouse;
            @event.IsHandled |= @event.IsInCategory(EventCategory.EventCategoryKeyboard) & io.WantCaptureKeyboard;
        }

        base.HandleEvent(@event);
    }

    public bool BlockEvents { get; set; }

    private static void OnConfigureIo()
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.WantSaveIniSettings = true;
    }
}