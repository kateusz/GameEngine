using System.Numerics;
using ImGuiNET;

namespace Editor.Panels;

public static class VectorPanel
{
    private static void DrawVectorControlHeader(string label, int componentCount, out float inputWidth)
    {
        // Setup columns (1/3 label, 2/3 controls)
        float totalWidth = ImGui.GetContentRegionAvail().X;
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * 0.33f);
        ImGui.SetColumnWidth(1, totalWidth * 0.67f);

        // Label column
        ImGui.Text(label);
        ImGui.NextColumn();

        // Calculate available width for each component (button + input)
        float columnWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = 20.0f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        float sectionWidth = columnWidth / componentCount;
        inputWidth = sectionWidth - (buttonWidth + spacing);
    }

    private static void DrawAxisControl(string axisLabel, ref float value, float resetValue, Vector4 color, float inputWidth, bool drag = true)
    {
        // Button colors
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * new Vector4(1.1f, 1.1f, 1.1f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);
        if (ImGui.Button(axisLabel, new Vector2(20.0f, ImGui.GetFrameHeight())))
            value = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        if (drag)
            ImGui.DragFloat($"##{axisLabel}", ref value, 0.1f, 0.0f, 0.0f, "%.2f");
        else
            ImGui.InputFloat($"##{axisLabel}", ref value);
    }

    public static void DrawVec3Control(string label, ref Vector3 values, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 3, out float inputWidth);

        // X, Y, Z
        DrawAxisControl("X", ref values.X, resetValue, new Vector4(0.8f, 0.1f, 0.15f, 1.0f), inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, new Vector4(0.2f, 0.7f, 0.2f, 1.0f), inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Z", ref values.Z, resetValue, new Vector4(0.1f, 0.25f, 0.8f, 1.0f), inputWidth);

        ImGui.PopID();
        ImGui.Columns(1);
    }

    public static void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 2, out float inputWidth);

        // X, Y
        DrawAxisControl("X", ref values.X, resetValue, new Vector4(0.8f, 0.1f, 0.15f, 1.0f), inputWidth, false);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, new Vector4(0.2f, 0.7f, 0.2f, 1.0f), inputWidth, false);

        ImGui.PopID();
        ImGui.Columns(1);
    }
}
