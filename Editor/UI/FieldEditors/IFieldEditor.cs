namespace Editor.UI.FieldEditors;

/// <summary>
/// Interface for field editors that render specific types in the script inspector.
/// Follows the strategy pattern to enable extensible type-specific rendering.
/// </summary>
public interface IFieldEditor
{
    /// <summary>
    /// Draws the editor UI for the field and returns true if the value was changed.
    /// </summary>
    /// <param name="label">The ImGui label for the field (should include unique ID)</param>
    /// <param name="value">The current field value (boxed)</param>
    /// <param name="newValue">The new value if changed</param>
    /// <returns>True if the value was modified by user interaction</returns>
    bool Draw(string label, object value, out object newValue);
}
