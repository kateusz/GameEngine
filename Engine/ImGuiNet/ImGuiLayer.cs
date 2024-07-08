using System.Numerics;
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

        io.Fonts.AddFontFromFileTTF("assets/fonts/opensans/OpenSans-Bold.ttf", 18.0f);
        //io.FontDefault = io.Fonts.AddFontFromFileTTF("assets/fonts/opensans/OpenSans-Regular.ttf", 18.0f);

        ImGui.StyleColorsDark();

        var style = ImGui.GetStyle();
        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            style.WindowRounding = 0.0f;
            style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
        }

        SetDarkThemeColors();
    }

    private static void SetDarkThemeColors()
    {
        var colors = ImGui.GetStyle().Colors;
        colors[(int)ImGuiCol.WindowBg] = new Vector4(
            0.1f, 0.105f, 0.11f, 1.0f
        );


        // Headers
        colors[(int)ImGuiCol.Header] = new Vector4(
            0.2f, 0.205f, 0.21f, 1.0f);

        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(
            0.3f, 0.305f, 0.31f, 1.0f);

        colors[(int)ImGuiCol.HeaderActive] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f
        );

        // Buttons
        colors[(int)ImGuiCol.Button] = new Vector4(
            0.2f, 0.205f, 0.21f, 1.0f
        );
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(
            0.3f, 0.305f, 0.31f, 1.0f
        );
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f
        );

        // Frame BG
        colors[(int)ImGuiCol.FrameBg] = new Vector4(
            0.2f, 0.205f, 0.21f, 1.0f
        );
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(
            0.3f, 0.305f, 0.31f, 1.0f
        );
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);

        // Tabs
        colors[(int)ImGuiCol.Tab] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(
            0.38f, 0.3805f, 0.381f, 1.0f);
        colors[(int)ImGuiCol.TabActive] = new Vector4(
            0.28f, 0.2805f, 0.281f, 1.0f);

        colors[(int)ImGuiCol.TabUnfocused] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(
            0.2f, 0.205f, 0.21f, 1.0f);
        // Title
        colors[(int)ImGuiCol.TitleBg] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(
            0.15f, 0.1505f, 0.151f, 1.0f);
    }
}