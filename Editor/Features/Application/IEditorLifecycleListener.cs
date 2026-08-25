namespace Editor.Features.Application;

public interface IEditorLifecycleListener
{
    void Attach();
    void Detach();
}
