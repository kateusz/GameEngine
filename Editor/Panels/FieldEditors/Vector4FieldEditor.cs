using System.Numerics;
using ImGuiNET;

namespace Editor.Panels.FieldEditors;

public class Vector4FieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector4)value;
        var changed = ImGui.ColorEdit4(label, ref v);
        newValue = v;
        return changed;
    }
}
