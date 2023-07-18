namespace Engine;

public class Application
{
    private readonly IWindow _window;
    private bool _isRunning;

    public Application(IWindow window)
    {
        _window = window;
    }

    public void Run()
    {
        _window.Run();
    }
}