using System.Numerics;
using Editor.Selection;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Elements;

public static class VectorPanel
{
    public static void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 2, out var inputWidth);
        
        DrawAxisControl("X", ref values.X, resetValue, EditorUIConstants.AxisXColor, inputWidth, false);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, EditorUIConstants.AxisYColor, inputWidth, false);

        ImGui.PopID();
        ImGui.Columns(1);
    }
    
    public static void DrawVec3Control(string label, ref Vector3 values, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 3, out var inputWidth);
        
        DrawAxisControl("X", ref values.X, resetValue, EditorUIConstants.AxisXColor, inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Y", ref values.Y, resetValue, EditorUIConstants.AxisYColor, inputWidth);
        ImGui.SameLine();
        DrawAxisControl("Z", ref values.Z, resetValue, EditorUIConstants.AxisZColor, inputWidth);

        ImGui.PopID();
        ImGui.Columns(1);
    }

    public static bool DrawVec3Control(string label, MixedVector3 mixed, ref Vector3 editBuffer, float resetValue = 0.0f)
    {
        ImGui.PushID(label);

        DrawVectorControlHeader(label, 3, out var inputWidth);

        var changed = false;
        changed |= DrawMixedAxisControl("X", mixed.X, ref editBuffer.X, resetValue, EditorUIConstants.AxisXColor, inputWidth);
        ImGui.SameLine();
        changed |= DrawMixedAxisControl("Y", mixed.Y, ref editBuffer.Y, resetValue, EditorUIConstants.AxisYColor, inputWidth);
        ImGui.SameLine();
        changed |= DrawMixedAxisControl("Z", mixed.Z, ref editBuffer.Z, resetValue, EditorUIConstants.AxisZColor, inputWidth);

        ImGui.PopID();
        ImGui.Columns(1);
        return changed;
    }
    
    private static void DrawVectorControlHeader(string label, int componentCount, out float inputWidth)
    {
        var totalWidth = ImGui.GetContentRegionAvail().X;
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * EditorUIConstants.PropertyLabelRatio);
        ImGui.SetColumnWidth(1, totalWidth * EditorUIConstants.PropertyInputRatio);
        
        ImGui.Text(label);
        ImGui.NextColumn();

        var columnWidth = ImGui.GetContentRegionAvail().X;
        var buttonWidth = EditorUIConstants.SmallButtonSize;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        var sectionWidth = columnWidth / componentCount;
        inputWidth = sectionWidth - (buttonWidth + spacing);
    }

    private static void DrawAxisControl(string axisLabel, ref float value, float resetValue, Vector4 color, float inputWidth, bool drag = true)
    {
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

    private static bool DrawMixedAxisControl(
        string axisLabel,
        float? commonValue,
        ref float editBuffer,
        float resetValue,
        Vector4 color,
        float inputWidth)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * new Vector4(1.1f, 1.1f, 1.1f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);

        if (ImGui.Button(axisLabel, new Vector2(EditorUIConstants.SmallButtonSize, ImGui.GetFrameHeight())))
        {
            editBuffer = resetValue;
            ImGui.PopStyleColor(3);
            return true;
        }

        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);

        if (commonValue.HasValue)
        {
            editBuffer = commonValue.Value;
            if (ImGui.DragFloat($"##{axisLabel}", ref editBuffer, 0.1f, 0.0f, 0.0f, "%.2f"))
                return true;
            return false;
        }

        var buffer = string.Empty;
        if (ImGui.InputText($"##{axisLabel}", ref buffer, 32, ImGuiInputTextFlags.CharsDecimal))
        {
            if (float.TryParse(buffer, out var parsed))
            {
                editBuffer = parsed;
                return true;
            }
        }

        return false;
    }
}
