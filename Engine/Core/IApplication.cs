namespace Engine.Core;

public interface IApplication
{
    void Run();
    void PushLayer(ILayer layer);
    void PushOverlay(ILayer overlay);
}