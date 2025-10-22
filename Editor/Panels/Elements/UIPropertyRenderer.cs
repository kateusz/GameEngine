using Editor.Panels.FieldEditors;
using ImGuiNET;

namespace Editor.Panels.Elements
{
    public static class UIPropertyRenderer
    {
        public static void DrawPropertyRow(string label, Action inputControl)
        {
            ImGui.Columns(2);
            ImGui.Text(label);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            inputControl();
            ImGui.Columns(1);
        }

        /// <summary>
        /// Draws a property field using the FieldEditorRegistry for type-specific rendering.
        /// Automatically handles common types (int, float, bool, string, Vector2/3/4) without boilerplate.
        /// </summary>
        /// <param name="label">Label to display for the property</param>
        /// <param name="value">Current value of the property</param>
        /// <param name="onValueChanged">Callback invoked when the value changes</param>
        /// <returns>True if the value was changed</returns>
        public static bool DrawPropertyField(string label, object value, Action<object> onValueChanged)
        {
            if (value == null)
                return false;

            var valueType = value.GetType();
            var editor = FieldEditorRegistry.GetEditor(valueType);

            if (editor == null)
            {
                // Fallback: display unsupported type message
                DrawPropertyRow(label, () =>
                {
                    ImGui.TextDisabled($"Unsupported type: {valueType.Name}");
                });
                return false;
            }

            bool changed = false;
            DrawPropertyRow(label, () =>
            {
                var inputLabel = $"##{label}";
                if (editor.Draw(inputLabel, value, out var newValue))
                {
                    onValueChanged(newValue);
                    changed = true;
                }
            });

            return changed;
        }
    }
} 