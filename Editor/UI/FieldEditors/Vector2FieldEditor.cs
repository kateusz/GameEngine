using System.Numerics;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class Vector2FieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector2)value;
        var changed = ImGui.DragFloat2(label, ref v);
        newValue = v;
        return changed;
    }
}
