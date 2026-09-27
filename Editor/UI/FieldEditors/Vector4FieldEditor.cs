using System.Numerics;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class Vector4FieldEditor : IFieldEditor
{
    public Type ValueType => typeof(Vector4);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector4)value;
        var changed = LayoutDrawer.DrawColorEdit4(label, ref v);
        newValue = v;
        return changed;
    }
}
