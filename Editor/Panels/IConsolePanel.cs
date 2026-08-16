namespace Editor.Panels;

public interface IConsolePanel : IDisposable
{
    void Initialize();
    void AddMessage(string message, ConsolePanel.LogLevel level = ConsolePanel.LogLevel.Info);
    void Draw();
    void Clear();
}
