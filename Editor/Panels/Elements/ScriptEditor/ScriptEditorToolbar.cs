using System.Numerics;
using Editor.Windows.ScriptEditor;
using ImGuiNET;

namespace Editor.Panels.Elements.ScriptEditor;

public class ScriptEditorToolbar
{
    public event Action SaveRequested;
    public event Action SaveAndCloseRequested;
    public event Action CloseRequested;

    public void Render(ScriptEditorState state)
    {
        if (ImGui.Button("Save", new Vector2(80, 0)))
        {
            Console.WriteLine("Save button clicked"); // Debug
            SaveRequested?.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("Save and Close", new Vector2(120, 0)))
        {
            Console.WriteLine("Save and Close button clicked"); // Debug
            SaveAndCloseRequested?.Invoke();
        }

        ImGui.SameLine();
        if (ImGui.Button("Close", new Vector2(80, 0)))
        {
            Console.WriteLine($"Close button clicked, HasChanges: {state.HasChanges}"); // Debug
            CloseRequested?.Invoke();
        }

        ImGui.Separator();
    }
}