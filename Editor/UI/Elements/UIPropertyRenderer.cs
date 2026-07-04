using Editor.UI.FieldEditors;
using ImGuiNET;

namespace Editor.UI.Elements;

public class UIPropertyRenderer(IEnumerable<IFieldEditor> editors)
{
    private readonly Dictionary<Type, IFieldEditor> _editors =
        editors.ToDictionary(editor => editor.ValueType);

    public static void DrawPropertyRow(string label, Action inputControl)
    {
        ImGui.Columns(2);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        inputControl();
        ImGui.Columns(1);
    }

    public bool TryDrawFieldEditor(string label, Type type, object value, out object newValue)
    {
        newValue = value;

        if (!_editors.TryGetValue(type, out var editor))
        {
            ImGui.TextDisabled($"Unsupported type: {type.Name}");
            return false;
        }

        editor.Draw(label, value, out newValue);
        return true;
    }

    public bool DrawPropertyField(string label, object? value, Action<object> onValueChanged)
    {
        if (value == null)
            return false;

        var valueType = value.GetType();
        if (!_editors.TryGetValue(valueType, out var editor))
        {
            DrawPropertyRow(label, () => ImGui.TextDisabled($"Unsupported type: {valueType.Name}"));
            return false;
        }

        var changed = false;
        DrawPropertyRow(label, () =>
        {
            var inputLabel = $"##{label}";
            editor.Draw(inputLabel, value, out var newValue);
            if (!EqualityComparer<object>.Default.Equals(value, newValue))
            {
                onValueChanged(newValue);
                changed = true;
            }
        });

        return changed;
    }
}
