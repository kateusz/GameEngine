using ImGuiNET;

namespace Editor.Panels.FieldEditors;

public class FloatFieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (float)value;
        var changed = ImGui.DragFloat(label, ref v);
        newValue = v;
        return changed;
    }
}
