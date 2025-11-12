using Engine.Core.Input;
using Serilog;

namespace Editor.Input;

/// <summary>
/// Centralized manager for keyboard shortcuts.
/// Handles registration, conflict detection, and execution of shortcuts.
/// </summary>
public class ShortcutManager
{
    private static readonly ILogger Logger = Log.ForContext<ShortcutManager>();

    private readonly List<KeyboardShortcut> _shortcuts = new();
    private readonly Dictionary<string, List<KeyboardShortcut>> _categorizedShortcuts = new();
    private readonly IReadOnlyList<KeyboardShortcut> _shortcutsReadOnly;
    private readonly IReadOnlyDictionary<string, List<KeyboardShortcut>> _categorizedShortcutsReadOnly;

    public ShortcutManager()
    {
        _shortcutsReadOnly = _shortcuts.AsReadOnly();
        _categorizedShortcutsReadOnly = _categorizedShortcuts.AsReadOnly();
    }

    /// <summary>
    /// Gets all registered shortcuts.
    /// </summary>
    public IReadOnlyList<KeyboardShortcut> Shortcuts => _shortcutsReadOnly;

    /// <summary>
    /// Gets shortcuts grouped by category.
    /// </summary>
    public IReadOnlyDictionary<string, List<KeyboardShortcut>> CategorizedShortcuts =>
        _categorizedShortcutsReadOnly;

    /// <summary>
    /// Registers a new keyboard shortcut.
    /// </summary>
    /// <param name="shortcut">The shortcut to register.</param>
    /// <param name="allowDuplicates">If false, logs a warning if a duplicate shortcut exists.</param>
    /// <returns>True if registered successfully, false if a duplicate was detected and not allowed.</returns>
    public bool RegisterShortcut(KeyboardShortcut shortcut, bool allowDuplicates = false)
    {
        // Check for conflicts
        var existingShortcut = _shortcuts.FirstOrDefault(s => s.Equals(shortcut));
        if (existingShortcut != null)
        {
            if (!allowDuplicates)
            {
                Logger.Warning(
                    "Shortcut conflict detected: {Shortcut} is already registered for '{ExistingDescription}'. " +
                    "Attempted to register for '{NewDescription}'.",
                    shortcut.GetDisplayString(),
                    existingShortcut.Description,
                    shortcut.Description);
                return false;
            }
        }

        _shortcuts.Add(shortcut);

        // Add to categorized dictionary
        if (!_categorizedShortcuts.ContainsKey(shortcut.Category))
        {
            _categorizedShortcuts[shortcut.Category] = new List<KeyboardShortcut>();
        }
        _categorizedShortcuts[shortcut.Category].Add(shortcut);

        Logger.Debug("Registered shortcut: {Shortcut} - {Description}",
            shortcut.GetDisplayString(), shortcut.Description);

        return true;
    }

    /// <summary>
    /// Registers multiple shortcuts at once.
    /// </summary>
    public void RegisterShortcuts(IEnumerable<KeyboardShortcut> shortcuts, bool allowDuplicates = false)
    {
        foreach (var shortcut in shortcuts)
        {
            RegisterShortcut(shortcut, allowDuplicates);
        }
    }

    /// <summary>
    /// Unregisters a shortcut.
    /// </summary>
    public bool UnregisterShortcut(KeyboardShortcut shortcut)
    {
        var removed = _shortcuts.Remove(shortcut);
        if (removed && _categorizedShortcuts.TryGetValue(shortcut.Category, out var categoryList))
        {
            categoryList.Remove(shortcut);
            if (categoryList.Count == 0)
            {
                _categorizedShortcuts.Remove(shortcut.Category);
            }
        }
        return removed;
    }

    /// <summary>
    /// Clears all registered shortcuts.
    /// </summary>
    public void Clear()
    {
        _shortcuts.Clear();
        _categorizedShortcuts.Clear();
        Logger.Debug("Cleared all shortcuts");
    }

    /// <summary>
    /// Processes a key press event and executes matching shortcuts.
    /// </summary>
    /// <param name="key">The pressed key.</param>
    /// <param name="control">Whether Control is pressed.</param>
    /// <param name="shift">Whether Shift is pressed.</param>
    /// <param name="alt">Whether Alt is pressed.</param>
    /// <returns>True if a shortcut was executed, false otherwise.</returns>
    public bool HandleKeyPress(KeyCodes key, bool control, bool shift, bool alt)
    {
        var matchingShortcuts = _shortcuts.Where(s => s.Matches(key, control, shift, alt)).ToList();

        if (matchingShortcuts.Count == 0)
        {
            return false;
        }

        // Execute all matching shortcuts (in case of duplicates allowed)
        foreach (var shortcut in matchingShortcuts)
        {
            try
            {
                Logger.Debug("Executing shortcut: {Shortcut}", shortcut.GetDisplayString());
                shortcut.Action.Invoke();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error executing shortcut {Shortcut}: {Message}",
                    shortcut.GetDisplayString(), ex.Message);
            }
        }

        return true;
    }

    /// <summary>
    /// Finds all shortcuts that conflict with the given key combination.
    /// </summary>
    public List<KeyboardShortcut> FindConflicts(KeyCodes key, KeyModifiers modifiers)
    {
        return _shortcuts
            .Where(s => s.Key == key && s.Modifiers == modifiers)
            .ToList();
    }

    /// <summary>
    /// Gets all shortcuts in a specific category.
    /// </summary>
    public List<KeyboardShortcut> GetShortcutsByCategory(string category)
    {
        return _categorizedShortcuts.TryGetValue(category, out var shortcuts)
            ? shortcuts.ToList()
            : new List<KeyboardShortcut>();
    }

    /// <summary>
    /// Gets all unique categories.
    /// </summary>
    public List<string> GetCategories()
    {
        return _categorizedShortcuts.Keys.OrderBy(k => k).ToList();
    }
}
