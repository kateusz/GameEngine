using System.Numerics;

namespace Editor.Panels.FieldEditors;

/// <summary>
/// Registry for field editors that handle rendering of different types in the script inspector.
/// Follows the strategy pattern for extensible type-specific rendering.
/// </summary>
public static class FieldEditorRegistry
{
    private static readonly Dictionary<Type, IFieldEditor> _editors = new()
    {
        { typeof(int), new IntFieldEditor() },
        { typeof(float), new FloatFieldEditor() },
        { typeof(double), new DoubleFieldEditor() },
        { typeof(bool), new BoolFieldEditor() },
        { typeof(string), new StringFieldEditor() },
        { typeof(Vector2), new Vector2FieldEditor() },
        { typeof(Vector3), new Vector3FieldEditor() },
        { typeof(Vector4), new Vector4FieldEditor() }
    };

    /// <summary>
    /// Gets the appropriate field editor for the given type.
    /// </summary>
    /// <param name="type">The type to get an editor for</param>
    /// <returns>The field editor if one exists, null otherwise</returns>
    public static IFieldEditor? GetEditor(Type type)
    {
        return _editors.TryGetValue(type, out var editor) ? editor : null;
    }

    /// <summary>
    /// Checks if an editor exists for the given type.
    /// </summary>
    /// <param name="type">The type to check</param>
    /// <returns>True if an editor exists, false otherwise</returns>
    public static bool HasEditor(Type type)
    {
        return _editors.ContainsKey(type);
    }
}
