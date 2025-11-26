using ECS;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.ComponentEditors;

public static class EntityNameEditor
{
    public static void Draw(Entity entity)
    {
        var tag = entity.Name;
        byte[] buffer = new byte[EditorUIConstants.MaxTextInputLength];
        Array.Clear(buffer, 0, buffer.Length);
        byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
        Array.Copy(tagBytes, buffer, Math.Min(tagBytes.Length, buffer.Length - 1));

        ImGui.Columns(2, "tag_columns", false);
        ImGui.SetColumnWidth(0, EditorUIConstants.DefaultColumnWidth);
        ImGui.Text("Tag");
        ImGui.NextColumn();
        ImGui.PushItemWidth(-1);
        
        if (ImGui.InputText("##TagInput", buffer, (uint)buffer.Length))
        {
            tag = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            entity.Name = tag;
        }

        ImGui.PopItemWidth();
        ImGui.Columns(1);
    }
}