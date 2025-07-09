using ImGuiNET;
using System;

namespace Editor.Panels
{
    public static class UIPropertyRenderer
    {
        public static void DrawPropertyRow(string label, Action inputControl)
        {
            ImGui.Columns(2);
            ImGui.Text(label);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            inputControl();
            ImGui.Columns(1);
        }
    }
} 