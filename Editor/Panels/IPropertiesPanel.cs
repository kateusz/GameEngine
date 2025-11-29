using ECS;

namespace Editor.Panels;

public interface IPropertiesPanel
{
    void Draw();
    void SetSelectedEntity(Entity? entity);
}
