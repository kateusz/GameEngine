using System.Numerics;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class Vector4FieldEditor : IFieldEditor
{
    public Type ValueType => typeof(Vector4);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector4)value;
        ImGui.ColorEdit4(label, ref v,
            ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB | ImGuiColorEditFlags.InputRGB |
            ImGuiColorEditFlags.NoOptions);
        newValue = v;
        return !v.Equals((Vector4)value);
    }
}
