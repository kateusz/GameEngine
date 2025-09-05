using System.Numerics;
using System.Runtime.InteropServices;
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
        
        // TODO: ImGuizmo CRASH
        //ImGuizmoWrapper.SetOrthographic(false);
        //ImGuizmoWrapper.BeginFrame();
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
        
        // TODO: ImGuizmo
        //var ctx = ImGui.GetCurrentContext();
        //ImGuizmoWrapper.SetImGuiContext(ctx);
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

        float fontSize = 18.0f;// *2.0f;
        io.Fonts.AddFontFromFileTTF("assets/fonts/opensans/OpenSans-Bold.ttf", fontSize);
        //io.FontDefault = io.Fonts.AddFontFromFileTTF("assets/fonts/opensans/OpenSans-Regular.ttf", fontSize);

        ImGui.StyleColorsDark();

        var style = ImGui.GetStyle();
        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            style.WindowRounding = 6.0f;
            style.ChildRounding = 6.0f;
            style.FrameRounding = 6.0f;
            style.PopupRounding = 6.0f;
            style.ScrollbarRounding = 12.0f;
            style.GrabRounding = 6.0f;
            style.TabRounding = 6.0f;
            style.WindowBorderSize = 1.0f;
            style.FrameBorderSize = 1.0f;
            style.PopupBorderSize = 1.0f;
            style.IndentSpacing = 18.0f;
            style.WindowPadding = new Vector2(12, 12);
            style.FramePadding = new Vector2(8, 4);
            style.ItemSpacing = new Vector2(8, 6);
            style.ItemInnerSpacing = new Vector2(6, 4);
            style.GrabMinSize = 20.0f;
            style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
        }

        SetDarkThemeColors();
    }

    private static void SetDarkThemeColors()
    {
        var colors = ImGui.GetStyle().Colors;
        // Modern dark background
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.13f, 0.14f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.13f, 0.14f, 0.17f, 0.98f);
        // Headers
        colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.28f, 0.30f, 0.37f, 1.0f);
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.22f, 0.24f, 0.29f, 1.0f);
        // Buttons
        colors[(int)ImGuiCol.Button] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.28f, 0.30f, 0.37f, 1.0f);
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.22f, 0.24f, 0.29f, 1.0f);
        // Frame BG
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.22f, 0.24f, 0.29f, 1.0f);
        // Tabs
        colors[(int)ImGuiCol.Tab] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.28f, 0.30f, 0.37f, 1.0f);
        colors[(int)ImGuiCol.TabActive] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.13f, 0.14f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        // Title
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.13f, 0.14f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.13f, 0.14f, 0.17f, 1.0f);
        // Accent (selection, slider, progress, etc.)
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.26f, 0.44f, 0.86f, 1.0f);
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        colors[(int)ImGuiCol.Separator] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.26f, 0.44f, 0.86f, 1.0f);
        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.13f, 0.14f, 0.17f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.20f, 0.22f, 0.27f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.28f, 0.30f, 0.37f, 1.0f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        // Text
        colors[(int)ImGuiCol.Text] = new Vector4(0.86f, 0.93f, 0.89f, 1.0f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.54f, 0.58f, 1.0f);
        // MenuBar
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.16f, 0.17f, 0.20f, 1.0f);
        // DragDrop
        colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        // Resize grip
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.36f, 0.54f, 0.96f, 0.2f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.36f, 0.54f, 0.96f, 0.7f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.36f, 0.54f, 0.96f, 0.9f);
        // Plot
        colors[(int)ImGuiCol.PlotLines] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
        colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.36f, 0.54f, 0.96f, 1.0f);
    }
}