using ECS;
using ImGuiNET;

namespace Editor.Panels.ComponentEditors;

public static class EntityNameEditor
{
    public static void Draw(Entity entity)
    {
        var tag = entity.Name;
        byte[] buffer = new byte[256];
        Array.Clear(buffer, 0, buffer.Length);
        byte[] tagBytes = System.Text.Encoding.UTF8.GetBytes(tag);
        Array.Copy(tagBytes, buffer, Math.Min(tagBytes.Length, buffer.Length - 1));

        ImGui.Columns(2, "tag_columns", false);
        ImGui.SetColumnWidth(0, 60.0f);
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