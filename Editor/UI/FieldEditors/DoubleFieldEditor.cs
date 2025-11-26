using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class DoubleFieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (double)value;
        var changed = ImGui.InputDouble(label, ref v);
        newValue = v;
        return changed;
    }
}
