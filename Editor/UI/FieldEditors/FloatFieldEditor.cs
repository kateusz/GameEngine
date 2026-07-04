using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class FloatFieldEditor : IFieldEditor
{
    public Type ValueType => typeof(float);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (float)value;
        var changed = ImGui.DragFloat(label, ref v);
        newValue = v;
        return changed;
    }
}
