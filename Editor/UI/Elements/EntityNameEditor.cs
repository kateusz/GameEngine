using ECS;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Elements;

public static class EntityNameEditor
{
    public static void Draw(Entity entity)
    {
        ImGui.Columns(2, "tag_columns", false);
        ImGui.SetColumnWidth(0, EditorUIConstants.DefaultColumnWidth);
        ImGui.Text("Name");
        ImGui.NextColumn();
        ImGui.PushItemWidth(-1);

        if (!MultiField.TryUniform(entity, e => e.Name, (a, b) => a == b, out var name))
        {
            var buffer = "";
            if (ImGui.InputText("##TagInput", ref buffer, EditorUIConstants.MaxTextInputLength,
                    ImGuiInputTextFlags.EnterReturnsTrue)
                || ImGui.IsItemDeactivatedAfterEdit())
                MultiField.WriteEach(entity, (Entity e, string n) => e.Name = n, buffer);
        }
        else if (ImGui.InputText("##TagInput", ref name, EditorUIConstants.MaxTextInputLength))
        {
            MultiField.WriteEach(entity, (Entity e, string n) => e.Name = n, name);
        }

        ImGui.PopItemWidth();
        ImGui.Columns(1);
    }
}
