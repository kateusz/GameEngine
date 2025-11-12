namespace Editor.Input;

/// <summary>
/// Represents keyboard modifier keys (Control, Shift, Alt) for keyboard shortcuts.
/// </summary>
public readonly struct KeyModifiers : IEquatable<KeyModifiers>
{
    /// <summary>
    /// Whether the Control key is required.
    /// </summary>
    public bool Control { get; init; }

    /// <summary>
    /// Whether the Shift key is required.
    /// </summary>
    public bool Shift { get; init; }

    /// <summary>
    /// Whether the Alt key is required.
    /// </summary>
    public bool Alt { get; init; }

    /// <summary>
    /// Initializes a new instance of KeyModifiers.
    /// </summary>
    public KeyModifiers(bool control = false, bool shift = false, bool alt = false)
    {
        Control = control;
        Shift = shift;
        Alt = alt;
    }

    /// <summary>
    /// No modifiers.
    /// </summary>
    public static readonly KeyModifiers None = new();

    /// <summary>
    /// Control key only.
    /// </summary>
    public static readonly KeyModifiers CtrlOnly = new(control: true);

    /// <summary>
    /// Shift key only.
    /// </summary>
    public static readonly KeyModifiers ShiftOnly = new(shift: true);

    /// <summary>
    /// Alt key only.
    /// </summary>
    public static readonly KeyModifiers AltOnly = new(alt: true);

    /// <summary>
    /// Control + Shift.
    /// </summary>
    public static readonly KeyModifiers CtrlShift = new(control: true, shift: true);

    /// <summary>
    /// Control + Alt.
    /// </summary>
    public static readonly KeyModifiers CtrlAlt = new(control: true, alt: true);

    /// <summary>
    /// Checks if all specified modifiers match the current state.
    /// </summary>
    public bool Matches(bool control, bool shift, bool alt)
    {
        return Control == control && Shift == shift && Alt == alt;
    }

    public bool Equals(KeyModifiers other)
    {
        return Control == other.Control && Shift == other.Shift && Alt == other.Alt;
    }

    public override bool Equals(object? obj)
    {
        return obj is KeyModifiers other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Control, Shift, Alt);
    }

    public override string ToString()
    {
        if (!Control && !Shift && !Alt)
            return "";

        var parts = new List<string>();
        if (Control) parts.Add("Ctrl");
        if (Shift) parts.Add("Shift");
        if (Alt) parts.Add("Alt");

        return string.Join("+", parts);
    }

    public static bool operator ==(KeyModifiers left, KeyModifiers right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(KeyModifiers left, KeyModifiers right)
    {
        return !left.Equals(right);
    }
}
