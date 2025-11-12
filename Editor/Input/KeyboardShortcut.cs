using Engine.Core.Input;

namespace Editor.Input;

/// <summary>
/// Represents a keyboard shortcut consisting of a key and optional modifiers.
/// </summary>
public class KeyboardShortcut : IEquatable<KeyboardShortcut>
{
    /// <summary>
    /// The primary key for this shortcut.
    /// </summary>
    public KeyCodes Key { get; }

    /// <summary>
    /// The modifier keys required for this shortcut.
    /// </summary>
    public KeyModifiers Modifiers { get; }

    /// <summary>
    /// Human-readable description of what this shortcut does.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Category for grouping shortcuts in UI (e.g., "File", "Edit", "View").
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// The action to execute when this shortcut is triggered.
    /// </summary>
    public Action Action { get; }

    /// <summary>
    /// Initializes a new keyboard shortcut.
    /// </summary>
    /// <param name="key">The primary key.</param>
    /// <param name="modifiers">The modifier keys.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="description">Description of the shortcut.</param>
    /// <param name="category">Category for organization.</param>
    public KeyboardShortcut(
        KeyCodes key,
        KeyModifiers modifiers,
        Action action,
        string description,
        string category = "General")
    {
        Key = key;
        Modifiers = modifiers;
        Action = action;
        Description = description;
        Category = category;
    }

    /// <summary>
    /// Checks if the given key and modifiers match this shortcut.
    /// </summary>
    public bool Matches(KeyCodes key, bool control, bool shift, bool alt)
    {
        return Key == key && Modifiers.Matches(control, shift, alt);
    }

    /// <summary>
    /// Returns a formatted string representation of the shortcut (e.g., "Ctrl+S").
    /// </summary>
    public string GetDisplayString()
    {
        var modifierString = Modifiers.ToString();
        var keyString = FormatKeyName(Key);

        if (string.IsNullOrEmpty(modifierString))
            return keyString;

        return $"{modifierString}+{keyString}";
    }

    /// <summary>
    /// Formats a key code into a readable string.
    /// </summary>
    private static string FormatKeyName(KeyCodes key)
    {
        return key switch
        {
            // Special formatting for common keys
            KeyCodes.Escape => "Esc",
            KeyCodes.Enter => "Enter",
            KeyCodes.Space => "Space",
            KeyCodes.Delete => "Del",
            KeyCodes.Backspace => "Backspace",
            KeyCodes.Tab => "Tab",

            // Function keys
            >= KeyCodes.F1 and <= KeyCodes.F12 => key.ToString(),

            // Number keys - remove the 'D' prefix
            >= KeyCodes.D0 and <= KeyCodes.D9 => key.ToString()[1..],

            // Letter keys - single letter
            >= KeyCodes.A and <= KeyCodes.Z => key.ToString(),

            // Default to enum name
            _ => key.ToString()
        };
    }

    public bool Equals(KeyboardShortcut? other)
    {
        if (other is null) return false;
        return Key == other.Key && Modifiers == other.Modifiers;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeyboardShortcut other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Modifiers);
    }

    public override string ToString()
    {
        return $"{GetDisplayString()} - {Description}";
    }
}
