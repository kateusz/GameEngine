using ECS;
using Editor.Features.Selection;
using Editor.Features.Viewport;
using Engine.Scene;
using SceneComponents.Rendering;

namespace Editor.Features.Application;

public sealed class SelectionViewportCoordinator(
    IEditorSelection selection,
    ISceneContext sceneContext,
    IEditorViewport editorViewport) : IEditorLifecycleListener
{
    private Action<Entity?, SelectionSource> _selectionChangedHandler = null!;

    public void Attach()
    {
        _selectionChangedHandler = OnSelectionChanged;
        selection.SelectionChanged += _selectionChangedHandler;
    }

    public void Detach() => selection.SelectionChanged -= _selectionChangedHandler;

    private void OnSelectionChanged(Entity? entity, SelectionSource source)
    {
        if (source != SelectionSource.Hierarchy || entity is null)
            return;
        if (sceneContext.ActiveScene is not { } scene)
            return;

        var focusTarget = entity;
        if (scene.GetParent(entity) is { } parent && parent.HasComponent<TileMapComponent>())
            focusTarget = parent;

        editorViewport.Camera.SetFocalPoint(scene.GetWorldPosition(focusTarget));
    }
}
