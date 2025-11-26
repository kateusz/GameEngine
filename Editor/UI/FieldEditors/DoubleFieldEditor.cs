using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class DoubleFieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (float)(double)value;
        var changed = ImGui.DragFloat(label, ref v);
        newValue = (double)v;
        return changed;
    }
}
