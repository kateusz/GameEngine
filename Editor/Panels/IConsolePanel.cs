namespace Editor.Panels;

public interface IConsolePanel : IDisposable
{
    void AddMessage(string message, ConsolePanel.LogLevel level = ConsolePanel.LogLevel.Info);
    void Draw();
    void Clear();
}
