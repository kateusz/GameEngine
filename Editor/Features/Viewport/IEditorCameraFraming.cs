using ECS;

namespace Editor.Features.Viewport;

public interface IEditorCameraFraming
{
    void SetCamera(EditorCamera camera);
    void FocusOnEntity(Entity entity, bool resetDistance = false);
}
