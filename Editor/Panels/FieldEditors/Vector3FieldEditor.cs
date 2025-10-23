using System.Numerics;
using ImGuiNET;

namespace Editor.Panels.FieldEditors;

public class Vector3FieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector3)value;
        var changed = ImGui.DragFloat3(label, ref v);
        newValue = v;
        return changed;
    }
}
