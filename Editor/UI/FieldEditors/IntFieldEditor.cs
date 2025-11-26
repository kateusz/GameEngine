using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class IntFieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (int)value;
        var changed = ImGui.DragInt(label, ref v);
        newValue = v;
        return changed;
    }
}
