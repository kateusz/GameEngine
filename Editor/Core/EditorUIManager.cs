using ImGuiNET;
using Serilog;

namespace Editor.Core;

/// <summary>
/// Centralized manager for all editor UI elements.
/// Handles registration, rendering order, visibility, focus management, and menu generation.
///
/// Benefits:
/// - Consistent lifecycle management across all UI components
/// - Automatic menu generation from registered components
/// - Focus tracking and event dispatching
/// - Easy to extend with new panels/windows/popups
/// </summary>
public class EditorUIManager
{
    private static readonly ILogger Logger = Log.ForContext<EditorUIManager>();

    private readonly Dictionary<string, IEditorPanel> _panels = new();
    private readonly Dictionary<string, IEditorWindow> _windows = new();
    private readonly List<IEditorPopup> _popups = new();

    private string? _focusedPanelId;

    /// <summary>
    /// Register a panel for lifecycle management
    /// </summary>
    public void RegisterPanel(IEditorPanel panel)
    {
        if (_panels.ContainsKey(panel.Id))
        {
            Logger.Warning("Panel with ID {PanelId} already registered. Overwriting.", panel.Id);
        }

        _panels[panel.Id] = panel;
        Logger.Debug("Registered panel: {PanelId} ({PanelTitle})", panel.Id, panel.Title);
    }

    /// <summary>
    /// Register a window for lifecycle management
    /// </summary>
    public void RegisterWindow(IEditorWindow window)
    {
        if (_windows.ContainsKey(window.Id))
        {
            Logger.Warning("Window with ID {WindowId} already registered. Overwriting.", window.Id);
        }

        _windows[window.Id] = window;
        Logger.Debug("Registered window: {WindowId} ({WindowTitle})", window.Id, window.Title);
    }

    /// <summary>
    /// Register a popup for lifecycle management
    /// </summary>
    public void RegisterPopup(IEditorPopup popup)
    {
        if (_popups.Any(p => p.Id == popup.Id))
        {
            Logger.Warning("Popup with ID {PopupId} already registered.", popup.Id);
            return;
        }

        _popups.Add(popup);
        Logger.Debug("Registered popup: {PopupId}", popup.Id);
    }

    /// <summary>
    /// Render all visible panels and track focus changes
    /// </summary>
    public void RenderAllPanels()
    {
        foreach (var panel in _panels.Values.Where(p => p.IsVisible))
        {
            panel.OnImGuiRender();

            // Track focus changes
            if (ImGui.IsWindowFocused() && _focusedPanelId != panel.Id)
            {
                // Notify previous panel of focus loss
                if (_focusedPanelId != null && _panels.TryGetValue(_focusedPanelId, out var previousPanel))
                {
                    previousPanel.OnUnfocus();
                }

                // Notify new panel of focus gain
                panel.OnFocus();
                _focusedPanelId = panel.Id;
            }
        }
    }

    /// <summary>
    /// Render all open windows
    /// </summary>
    /// <param name="viewportDockId">Optional dock ID to pass to windows for docking</param>
    public void RenderAllWindows(uint viewportDockId = 0)
    {
        foreach (var window in _windows.Values.Where(w => w.IsOpen))
        {
            window.OnImGuiRender(viewportDockId);
        }
    }

    /// <summary>
    /// Render all open popups
    /// </summary>
    public void RenderAllPopups()
    {
        foreach (var popup in _popups.Where(p => p.IsOpen))
        {
            popup.OnImGuiRender();
        }
    }

    /// <summary>
    /// Render a "View" menu with all registered panels and windows
    /// </summary>
    /// <param name="menuName">Name of the menu (default: "View")</param>
    public void RenderViewMenu(string menuName = "View")
    {
        if (ImGui.BeginMenu(menuName))
        {
            // Render panel toggles
            if (_panels.Count > 0)
            {
                ImGui.TextDisabled("Panels");
                ImGui.Separator();

                foreach (var panel in _panels.Values.OrderBy(p => p.Title))
                {
                    if (ImGui.MenuItem(panel.Title, null, panel.IsVisible))
                    {
                        panel.IsVisible = !panel.IsVisible;
                    }
                }
            }

            if (_panels.Count > 0 && _windows.Count > 0)
            {
                ImGui.Separator();
            }

            // Render window show/open options
            if (_windows.Count > 0)
            {
                ImGui.TextDisabled("Windows");
                ImGui.Separator();

                foreach (var window in _windows.Values.OrderBy(w => w.Title))
                {
                    var isOpen = window.IsOpen;
                    if (ImGui.MenuItem(window.Title, null, isOpen))
                    {
                        if (!isOpen)
                        {
                            window.IsOpen = true;
                            window.OnOpen();
                        }
                        else
                        {
                            window.IsOpen = false;
                            window.OnClose();
                        }
                    }
                }
            }

            ImGui.EndMenu();
        }
    }

    /// <summary>
    /// Get a specific panel by type
    /// </summary>
    public T? GetPanel<T>() where T : class, IEditorPanel
    {
        return _panels.Values.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Get a specific window by type
    /// </summary>
    public T? GetWindow<T>() where T : class, IEditorWindow
    {
        return _windows.Values.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Get a specific popup by type
    /// </summary>
    public T? GetPopup<T>() where T : class, IEditorPopup
    {
        return _popups.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Get a panel by its ID
    /// </summary>
    public IEditorPanel? GetPanelById(string id)
    {
        return _panels.TryGetValue(id, out var panel) ? panel : null;
    }

    /// <summary>
    /// Get a window by its ID
    /// </summary>
    public IEditorWindow? GetWindowById(string id)
    {
        return _windows.TryGetValue(id, out var window) ? window : null;
    }

    /// <summary>
    /// Get currently focused panel ID
    /// </summary>
    public string? GetFocusedPanelId() => _focusedPanelId;

    /// <summary>
    /// Show a window by type
    /// </summary>
    public void ShowWindow<T>() where T : class, IEditorWindow
    {
        var window = GetWindow<T>();
        if (window != null)
        {
            window.IsOpen = true;
            window.OnOpen();
        }
    }

    /// <summary>
    /// Hide a window by type
    /// </summary>
    public void HideWindow<T>() where T : class, IEditorWindow
    {
        var window = GetWindow<T>();
        if (window != null)
        {
            window.IsOpen = false;
            window.OnClose();
        }
    }

    /// <summary>
    /// Show a popup by type
    /// </summary>
    public void ShowPopup<T>() where T : class, IEditorPopup
    {
        var popup = GetPopup<T>();
        popup?.Show();
    }

    /// <summary>
    /// Get all registered panels
    /// </summary>
    public IEnumerable<IEditorPanel> GetAllPanels() => _panels.Values;

    /// <summary>
    /// Get all registered windows
    /// </summary>
    public IEnumerable<IEditorWindow> GetAllWindows() => _windows.Values;

    /// <summary>
    /// Get all registered popups
    /// </summary>
    public IEnumerable<IEditorPopup> GetAllPopups() => _popups;
}
