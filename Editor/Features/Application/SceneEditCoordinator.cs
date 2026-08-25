using Editor.Features.History;
using Editor.Features.Scene;
using Engine.Scene;

namespace Editor.Features.Application;

public sealed class SceneEditCoordinator(
    ISceneContext sceneContext,
    ISceneHierarchyPanel hierarchy,
    SceneToolbar toolbar,
    IEditorHistory history) : IEditorLifecycleListener
{
    private Action<IScene> _sceneChangedHandler = null!;

    public void Attach()
    {
        _sceneChangedHandler = scene =>
        {
            hierarchy.SetScene(scene);
            toolbar.ApplyGridFromScene(scene);
            history.Clear();
        };

        sceneContext.SceneChanged += _sceneChangedHandler;
    }

    public void Detach() => sceneContext.SceneChanged -= _sceneChangedHandler;
}
