using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Elements;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class Vector3FieldEditor : IFieldEditor
{
    public Type ValueType => typeof(Vector3);

    public bool Draw(string label, object value, out object newValue)
    {
        var v = (Vector3)value;
        var before = v;
        var width = VectorPanel.ComputeAxisInputWidth(3);
        ImGui.PushID(label);
        VectorPanel.DrawAxisControl("X", ref v.X, 0f, EditorUIConstants.AxisXColor, width);
        ImGui.SameLine();
        VectorPanel.DrawAxisControl("Y", ref v.Y, 0f, EditorUIConstants.AxisYColor, width);
        ImGui.SameLine();
        VectorPanel.DrawAxisControl("Z", ref v.Z, 0f, EditorUIConstants.AxisZColor, width);
        ImGui.PopID();
        newValue = v;
        return !MultiField.SameFloat(before.X, v.X)
               || !MultiField.SameFloat(before.Y, v.Y)
               || !MultiField.SameFloat(before.Z, v.Z);
    }
}
