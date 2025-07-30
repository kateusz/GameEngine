using System.Numerics;
using Editor.Windows.ScriptEditor;
using ImGuiNET;

namespace Editor.Panels.Elements.ScriptEditor;

public class ScriptEditorStatusBar
{
    public void Render(ScriptEditorState state)
    {
        ImGui.BeginChild("StatusBar", new Vector2(-1, 20));

        ImGui.Text(state.HasChanges ? "Modified" : "Saved");

        ImGui.SameLine(ImGui.GetWindowWidth() - 350);
        ImGui.Text($"Characters: {state.ScriptContent.Length} | Shortcuts: Ctrl+C (Copy), Ctrl+V (Paste), Ctrl+X (Cut)");

        ImGui.EndChild();
    }
}