using Engine.Core;
using Engine.Events;
using ImGuiNET;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES.Extensions.ImGui;

namespace Engine.ImGuiNet;

public class ImGuiLayer : Layer
{
    private readonly ImGuiController _controller;


    // UI state
    private static float _f = 0.0f;
    private static int _counter = 0;
    private static int _dragInt = 0;
    private static Vector3 _clearColor = new Vector3(0.45f, 0.55f, 0.6f);
    private static bool _showImGuiDemoWindow = true;
    private static bool _showAnotherWindow = false;
    private static bool _showMemoryEditor = false;
    private static byte[] _memoryEditorData;
    private static uint s_tab_bar_flags = (uint)ImGuiTabBarFlags.Reorderable;
    static bool[] s_opened = { true, true, true, true }; // Persistent user state

    public ImGuiLayer(string name) : base(name)
    {
        //_controller = new ImGuiController();
    }

    public override void OnImGuiRender()
    {
        ImGuiNET.ImGui.DockSpaceOverViewport();
        ImGui.ShowDemoWindow();
        //SubmitUI();
    }

    private void SubmitUI()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(100, 500));
        ImGui.Text("");
        ImGui.Text(string.Empty);
        ImGui.Text("Hello, world!");                                        // Display some text (you can use a format string too)
        ImGui.SliderFloat("float", ref _f, 0, 1, _f.ToString("0.000"));  // Edit 1 float using a slider from 0.0f to 1.0f    
        //ImGui.ColorEdit3("clear color", ref _clearColor);                   // Edit 3 floats representing a color

        ImGui.Text($"Mouse position: {ImGui.GetMousePos()}");

        ImGui.Checkbox("ImGui Demo Window", ref _showImGuiDemoWindow);                 // Edit bools storing our windows open/close state
        ImGui.Checkbox("Another Window", ref _showAnotherWindow);
        ImGui.Checkbox("Memory Editor", ref _showMemoryEditor);
        if (ImGui.Button("Button"))                                         // Buttons return true when clicked (NB: most widgets return true when edited/activated)
            _counter++;
        ImGui.SameLine(0, -1);
        ImGui.Text($"counter = {_counter}");

        ImGui.DragInt("Draggable Int", ref _dragInt);

        float framerate = ImGui.GetIO().Framerate;
        ImGui.Text($"Application average {1000.0f / framerate:0.##} ms/frame ({framerate:0.#} FPS)");
        ImGui.End();
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
            //todo
            //_controller.WindowResized(windowResizeEvent.Width, windowResizeEvent.Height);
        }
        base.HandleEvent(@event);
    }
}