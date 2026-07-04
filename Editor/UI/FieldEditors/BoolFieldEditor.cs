using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class BoolFieldEditor : IFieldEditor
{
    public Type ValueType => typeof(bool);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (bool)value;
        var changed = ImGui.Checkbox(label, ref v);
        newValue = v;
        return changed;
    }
}
