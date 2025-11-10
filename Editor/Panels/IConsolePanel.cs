namespace Editor.Panels;

/// <summary>
/// Interface for the console panel that displays log messages.
/// </summary>
public interface IConsolePanel
{
    /// <summary>
    /// Adds a message to the console log.
    /// </summary>
    /// <param name="message">Message text</param>
    /// <param name="level">Log level (Info, Warning, Error)</param>
    void AddMessage(string message, LogLevel level = LogLevel.Info);

    /// <summary>
    /// Renders the console panel using ImGui.
    /// </summary>
    void Draw();

    /// <summary>
    /// Clears all messages from the console.
    /// </summary>
    void Clear();
}
