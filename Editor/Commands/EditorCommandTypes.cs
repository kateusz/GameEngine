namespace Editor.Commands;

public readonly record struct CanExecuteResult(bool Allowed, string? Reason = null)
{
    public static CanExecuteResult Yes { get; } = new(true);
    public static CanExecuteResult No(string reason) => new(false, reason);
}

public sealed class EditorCommand(
    string id,
    string title,
    string category,
    Action execute,
    Func<CanExecuteResult>? canExecute = null,
    string? shortcutDisplay = null)
{
    public string Id { get; } = id;
    public string Title { get; } = title;
    public string Category { get; } = category;
    public string? ShortcutDisplay { get; } = shortcutDisplay;
    public Action Execute { get; } = execute;
    public Func<CanExecuteResult> CanExecute { get; } = canExecute ?? (() => CanExecuteResult.Yes);
}

public sealed record CommandPaletteItem(
    string Id,
    string Title,
    string Category,
    string? ShortcutDisplay,
    bool Enabled,
    string? DisabledReason);
