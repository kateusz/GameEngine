using ECS;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.ComponentEditors;

public static class EntityNameEditor
{
    public static void Draw(Entity entity)
    {
        var name = entity.Name;

        ImGui.Columns(2, "tag_columns", false);
        ImGui.SetColumnWidth(0, EditorUIConstants.DefaultColumnWidth);
        ImGui.Text("Tag");
        ImGui.NextColumn();
        ImGui.PushItemWidth(-1);

        if (ImGui.InputText("##TagInput", ref name, EditorUIConstants.MaxTextInputLength))
            entity.Name = name;

        ImGui.PopItemWidth();
        ImGui.Columns(1);
    }
}