namespace Editor.Panels;

/// <summary>
/// Interface for the content browser panel that displays project assets.
/// </summary>
public interface IContentBrowserPanel
{
    /// <summary>
    /// Initializes the content browser panel and loads icons.
    /// </summary>
    void Init();

    /// <summary>
    /// Renders the content browser panel using ImGui.
    /// </summary>
    void Draw();

    /// <summary>
    /// Sets the root directory for the content browser.
    /// </summary>
    /// <param name="rootDir">Path to the root directory</param>
    void SetRootDirectory(string rootDir);
}
