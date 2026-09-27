using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Elements;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class Vector2FieldEditor : IFieldEditor
{
    public Type ValueType => typeof(Vector2);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector2)value;
        var before = v;
        var width = VectorPanel.ComputeAxisInputWidth(2);
        ImGui.PushID(label);
        VectorPanel.DrawAxisControl("X", ref v.X, 0f, EditorUIConstants.AxisXColor, width, drag: false);
        ImGui.SameLine();
        VectorPanel.DrawAxisControl("Y", ref v.Y, 0f, EditorUIConstants.AxisYColor, width, drag: false);
        ImGui.PopID();
        newValue = v;
        return !MultiField.SameFloat(before.X, v.X) || !MultiField.SameFloat(before.Y, v.Y);
    }
}
