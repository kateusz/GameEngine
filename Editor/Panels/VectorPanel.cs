using System.Numerics;
using ImGuiNET;
using Editor.UI.Constants;

namespace Editor.Panels;

public static class VectorPanel
{
    private static void DrawVectorControlHeader(string label, int componentCount, out float inputWidth)
    {
        // Setup columns (1/3 label, 2/3 controls)
        float totalWidth = ImGui.GetContentRegionAvail().X;
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * EditorUIConstants.PropertyLabelRatio);
        ImGui.SetColumnWidth(1, totalWidth * EditorUIConstants.PropertyInputRatio);

        // Label column
        ImGui.Text(label);
        ImGui.NextColumn();

        // Calculate available width for each component (button + input)
        float columnWidth = ImGui.GetContentRegionAvail().X;
        float buttonWidth = EditorUIConstants.SmallButtonSize;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        float sectionWidth = columnWidth / componentCount;
        inputWidth = sectionWidth - (buttonWidth + spacing);
    }

    private static void DrawAxisControl(string axisLabel, ref float value, float resetValue, Vector4 color, float inputWidth, bool drag = true)
    {
        // Use colored button for axis reset buttons
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * new Vector4(1.1f, 1.1f, 1.1f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);

        if (ImGui.Button(axisLabel, new Vector2(EditorUIConstants.SmallButtonSize, ImGui.GetFrameHeight())))
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
        DrawAxisControl("X", ref values.X, resetValue, EditorUIConstants.AxisXColor, inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, EditorUIConstants.AxisYColor, inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Z", ref values.Z, resetValue, EditorUIConstants.AxisZColor, inputWidth);

        ImGui.PopID();
        ImGui.Columns(1);
    }

    public static void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 2, out float inputWidth);

        // X, Y
        DrawAxisControl("X", ref values.X, resetValue, EditorUIConstants.AxisXColor, inputWidth, false);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, EditorUIConstants.AxisYColor, inputWidth, false);

        ImGui.PopID();
        ImGui.Columns(1);
    }
}
