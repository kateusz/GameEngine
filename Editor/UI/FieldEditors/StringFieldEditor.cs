using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.FieldEditors;

public class StringFieldEditor : IFieldEditor
{
    public bool Draw(string label, object value, out object newValue)
    {
        var v = (string)value ?? string.Empty;
        var changed = ImGui.InputText(label, ref v, EditorUIConstants.MaxTextInputLength);
        newValue = v;
        return changed;
    }
}
