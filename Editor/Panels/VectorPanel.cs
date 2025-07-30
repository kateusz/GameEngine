using System.Numerics;
using ImGuiNET;

namespace Editor.Panels;

public static class VectorPanel
{
    public static void DrawVec3Control(string label, ref Vector3 values, float resetValue = 0.0f)
    {
        // Calculate full row width before columns
        var totalWidth = ImGui.GetContentRegionAvail().X;

        // Setup columns (1/3 label, 2/3 controls)
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * 0.33f);
        ImGui.SetColumnWidth(1, totalWidth * 0.67f);

        // Label
        ImGui.Text(label);
        ImGui.NextColumn();

        // Controls
        ImGui.PushID(label);

        var columnWidth = ImGui.GetContentRegionAvail().X;

        // Each axis: button + float input
        var buttonWidth = 20.0f;
        var spacing = ImGui.GetStyle().ItemSpacing.X;

        // Split 3 equal sections in the control column
        var sectionWidth = columnWidth / 3.0f;
        var inputWidth = sectionWidth - (buttonWidth + spacing);

        // ---------- X ----------
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", new Vector2(buttonWidth, ImGui.GetFrameHeight())))
            values.X = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.DragFloat("##X", ref values.X, 0.1f, 0.0f, 0.0f, "%.2f");

        // ---------- Y ----------
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        if (ImGui.Button("Y", new Vector2(buttonWidth, ImGui.GetFrameHeight())))
            values.Y = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.DragFloat("##Y", ref values.Y, 0.1f, 0.0f, 0.0f, "%.2f");

        // ---------- Z ----------
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.2f, 0.35f, 0.9f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.1f, 0.25f, 0.8f, 1.0f));
        if (ImGui.Button("Z", new Vector2(buttonWidth, ImGui.GetFrameHeight())))
            values.Z = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.DragFloat("##Z", ref values.Z, 0.1f, 0.0f, 0.0f, "%.2f");

        ImGui.PopID();

        // Reset columns for the next row
        ImGui.Columns(1);
    }
    
    public static void DrawVec2Control(string label, ref Vector2 values, float resetValue = 0.0f)
    {
        // 1) Get total available width BEFORE columns
        float totalWidth = ImGui.GetContentRegionAvail().X;

        // 2) Setup columns (1/3 label, 2/3 controls)
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * 0.33f);
        ImGui.SetColumnWidth(1, totalWidth * 0.67f);

        // 3) Label column
        ImGui.Text(label);
        ImGui.NextColumn();

        // 4) Controls column
        ImGui.PushID(label);

        float columnWidth = ImGui.GetContentRegionAvail().X;

        float buttonWidth = 20.0f;
        float spacing = ImGui.GetStyle().ItemSpacing.X;

        // Split control column into 2 equal sections
        float sectionWidth = columnWidth / 2.0f;
        float inputWidth = sectionWidth - (buttonWidth + spacing);

        // ----- X -----
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.9f, 0.2f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.8f, 0.1f, 0.15f, 1.0f));
        if (ImGui.Button("X", new Vector2(buttonWidth, ImGui.GetFrameHeight())))
            values.X = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.InputFloat("##X", ref values.X);

        // ----- Y -----
        ImGui.SameLine();
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.8f, 0.3f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.2f, 0.7f, 0.2f, 1.0f));
        if (ImGui.Button("Y", new Vector2(buttonWidth, ImGui.GetFrameHeight())))
            values.Y = resetValue;
        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        ImGui.InputFloat("##Y", ref values.Y);

        ImGui.PopID();

        // 5) Reset columns
        ImGui.Columns(1);
    }
}