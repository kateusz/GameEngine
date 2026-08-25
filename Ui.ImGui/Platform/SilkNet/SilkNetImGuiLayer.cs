using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;
using Engine.Platform.SilkNet;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Ui.ImGui.Platform.SilkNet;

internal sealed class SilkNetImGuiLayer : IImGuiLayer, IDisposable
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr GetClipboardTextFn(IntPtr userData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetClipboardTextFn(IntPtr userData, IntPtr text);

    private static IKeyboard? _clipboardKeyboard;
    private static IntPtr _clipboardUtf8;

    private ImGuiController? _controller;
    private IInputContext? _inputContext;
    private GetClipboardTextFn? _getClipboardTextFn;
    private SetClipboardTextFn? _setClipboardTextFn;
    private bool _disposed;

    public void OnDetach()
    {
        Dispose();
    }

    public void OnUpdate(TimeSpan timeSpan)
    {
    }

    public void Draw()
    {
    }

    public void BeginFrame(TimeSpan elapsed)
    {
        SyncModifierKeys();
        _controller?.Update((float)elapsed.TotalSeconds);
    }

    public void EndFrame()
    {
        _controller?.Render();
    }

    public void OnAttach(IInputSystem inputSystem)
    {
        var view = SilkNetContext.Window;
        var inputContext = inputSystem.Context;
        var gl = SilkNetContext.GL;

        _inputContext = inputContext;
        _controller = new ImGuiController(gl, view, inputContext, OnConfigureIo);

        if (inputContext.Keyboards.Count > 0)
        {
            _clipboardKeyboard = inputContext.Keyboards[0];
            _getClipboardTextFn = GetClipboardText;
            _setClipboardTextFn = SetClipboardText;
            var io = ImGuiNET.ImGui.GetIO();
            io.GetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_getClipboardTextFn);
            io.SetClipboardTextFn = Marshal.GetFunctionPointerForDelegate(_setClipboardTextFn);
        }
    }

    private static IntPtr GetClipboardText(IntPtr userData)
    {
        string text;
        try { text = _clipboardKeyboard?.ClipboardText ?? string.Empty; }
        catch { text = string.Empty; } // GLFW: non-text clipboard

        if (_clipboardUtf8 != IntPtr.Zero)
            Marshal.FreeCoTaskMem(_clipboardUtf8);
        return _clipboardUtf8 = Marshal.StringToCoTaskMemUTF8(text);
    }

    private static void SetClipboardText(IntPtr userData, IntPtr text)
    {
        if (_clipboardKeyboard is null)
            return;

        _clipboardKeyboard.ClipboardText = Marshal.PtrToStringUTF8(text) ?? string.Empty;
    }

    private void SyncModifierKeys()
    {
        if (_inputContext?.Keyboards.Count is not > 0)
            return;

        var keyboard = _inputContext.Keyboards[0];
        var io = ImGuiNET.ImGui.GetIO();
        io.AddKeyEvent(ImGuiKey.ModCtrl,
            keyboard.IsKeyPressed(Key.ControlLeft) || keyboard.IsKeyPressed(Key.ControlRight));
        io.AddKeyEvent(ImGuiKey.ModShift,
            keyboard.IsKeyPressed(Key.ShiftLeft) || keyboard.IsKeyPressed(Key.ShiftRight));
        io.AddKeyEvent(ImGuiKey.ModAlt,
            keyboard.IsKeyPressed(Key.AltLeft) || keyboard.IsKeyPressed(Key.AltRight));
        io.AddKeyEvent(ImGuiKey.ModSuper,
            keyboard.IsKeyPressed(Key.SuperLeft) || keyboard.IsKeyPressed(Key.SuperRight));
    }

    public void HandleWindowEvent(WindowEvent windowEvent)
    {
        if (windowEvent is WindowCloseEvent)
            Dispose();
    }

    public void HandleInputEvent(InputEvent windowEvent)
    {
        // Never block key/button releases — held state (and scripts) must see key-up.
        if (windowEvent is KeyReleasedEvent or MouseButtonReleasedEvent)
            return;

        var io = ImGuiNET.ImGui.GetIO();
        if (windowEvent.IsInCategory(EventCategory.EventCategoryKeyboard) && io.WantCaptureKeyboard)
            windowEvent.IsHandled = true;
    }

    private static void OnConfigureIo()
    {
        var io = ImGuiNET.ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.WantSaveIniSettings = true;

        var fontSize = 15.0f;
        io.Fonts.AddFontFromFileTTF("assets/fonts/opensans/OpenSans-Bold.ttf", fontSize);

        SetupImGuiStyle();
    }

    // Dark Ruda / Raikiri from ImThemes
    private static void SetupImGuiStyle()
    {
        var style = ImGuiNET.ImGui.GetStyle();

        style.Alpha = 1.0f;
        style.DisabledAlpha = 0.6f;
        style.WindowPadding = new Vector2(8.0f, 8.0f);
        style.WindowRounding = 0.0f;
        style.WindowBorderSize = 1.0f;
        style.WindowMinSize = new Vector2(32.0f, 32.0f);
        style.WindowTitleAlign = new Vector2(0.0f, 0.5f);
        style.WindowMenuButtonPosition = ImGuiDir.Left;
        style.ChildRounding = 0.0f;
        style.ChildBorderSize = 1.0f;
        style.PopupRounding = 0.0f;
        style.PopupBorderSize = 1.0f;
        style.FramePadding = new Vector2(4.0f, 3.0f);
        style.FrameRounding = 4.0f;
        style.FrameBorderSize = 0.0f;
        style.ItemSpacing = new Vector2(8.0f, 4.0f);
        style.ItemInnerSpacing = new Vector2(4.0f, 4.0f);
        style.CellPadding = new Vector2(4.0f, 2.0f);
        style.IndentSpacing = 21.0f;
        style.ColumnsMinSpacing = 6.0f;
        style.ScrollbarSize = 14.0f;
        style.ScrollbarRounding = 9.0f;
        style.GrabMinSize = 10.0f;
        style.GrabRounding = 4.0f;
        style.TabRounding = 4.0f;
        style.TabBorderSize = 0.0f;
        style.TabMinWidthForCloseButton = 0.0f;
        style.ColorButtonPosition = ImGuiDir.Right;
        style.ButtonTextAlign = new Vector2(0.5f, 0.5f);
        style.SelectableTextAlign = new Vector2(0.0f, 0.0f);

        style.Colors[(int)ImGuiCol.Text] = new Vector4(0.9490196f, 0.95686275f, 0.9764706f, 1.0f);
        style.Colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.35686275f, 0.41960785f, 0.46666667f, 1.0f);
        style.Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.10980392f, 0.14901961f, 0.16862746f, 1.0f);
        style.Colors[(int)ImGuiCol.ChildBg] = new Vector4(0.14901961f, 0.1764706f, 0.21960784f, 1.0f);
        style.Colors[(int)ImGuiCol.PopupBg] = new Vector4(0.078431375f, 0.078431375f, 0.078431375f, 0.94f);
        style.Colors[(int)ImGuiCol.Border] = new Vector4(0.078431375f, 0.09803922f, 0.11764706f, 1.0f);
        style.Colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.11764706f, 0.2f, 0.2784314f, 1.0f);
        style.Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.08627451f, 0.11764706f, 0.13725491f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBg] = new Vector4(0.08627451f, 0.11764706f, 0.13725491f, 0.65f);
        style.Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.078431375f, 0.09803922f, 0.11764706f, 1.0f);
        style.Colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.0f, 0.0f, 0.0f, 0.51f);
        style.Colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.14901961f, 0.1764706f, 0.21960784f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.019607844f, 0.019607844f, 0.019607844f, 0.39f);
        style.Colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.1764706f, 0.21960784f, 0.24705882f, 1.0f);
        style.Colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.08627451f, 0.20784314f, 0.30980393f, 1.0f);
        style.Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.2784314f, 0.5568628f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.2784314f, 0.5568628f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.36862746f, 0.60784316f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.Button] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.2784314f, 0.5568628f, 1.0f, 1.0f);
        style.Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.05882353f, 0.5294118f, 0.9764706f, 1.0f);
        style.Colors[(int)ImGuiCol.Header] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 0.55f);
        style.Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.8f);
        style.Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 1.0f);
        style.Colors[(int)ImGuiCol.Separator] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 1.0f);
        style.Colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.09803922f, 0.4f, 0.7490196f, 0.78f);
        style.Colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.09803922f, 0.4f, 0.7490196f, 1.0f);
        style.Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.25f);
        style.Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.67f);
        style.Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.95f);
        style.Colors[(int)ImGuiCol.Tab] = new Vector4(0.10980392f, 0.14901961f, 0.16862746f, 1.0f);
        style.Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.8f);
        style.Colors[(int)ImGuiCol.TabActive] = new Vector4(0.2f, 0.24705882f, 0.28627452f, 1.0f);
        style.Colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.10980392f, 0.14901961f, 0.16862746f, 1.0f);
        style.Colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.10980392f, 0.14901961f, 0.16862746f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLines] = new Vector4(0.60784316f, 0.60784316f, 0.60784316f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(1.0f, 0.42745098f, 0.34901962f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.8980392f, 0.69803923f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(1.0f, 0.6f, 0.0f, 1.0f);
        style.Colors[(int)ImGuiCol.TableHeaderBg] = new Vector4(0.1882353f, 0.1882353f, 0.2f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderStrong] = new Vector4(0.30980393f, 0.30980393f, 0.34901962f, 1.0f);
        style.Colors[(int)ImGuiCol.TableBorderLight] = new Vector4(0.22745098f, 0.22745098f, 0.24705882f, 1.0f);
        style.Colors[(int)ImGuiCol.TableRowBg] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        style.Colors[(int)ImGuiCol.TableRowBgAlt] = new Vector4(1.0f, 1.0f, 1.0f, 0.06f);
        style.Colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 0.35f);
        style.Colors[(int)ImGuiCol.DragDropTarget] = new Vector4(1.0f, 1.0f, 0.0f, 0.9f);
        style.Colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.25882354f, 0.5882353f, 0.9764706f, 1.0f);
        style.Colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.0f, 1.0f, 1.0f, 0.7f);
        style.Colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.8f, 0.8f, 0.8f, 0.2f);
        style.Colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.8f, 0.8f, 0.8f, 0.35f);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _controller?.Dispose();
        _controller = null!;
        _clipboardKeyboard = null;
        if (_clipboardUtf8 != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(_clipboardUtf8);
            _clipboardUtf8 = IntPtr.Zero;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
