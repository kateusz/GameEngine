using Engine.Scene;

namespace Editor.Features.Scene;

public interface ISceneHierarchyPanel
{
    void Draw();
    void SetScene(IScene scene);
}
