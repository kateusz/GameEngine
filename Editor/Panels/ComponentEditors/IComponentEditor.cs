using ECS;

namespace Editor.Panels.ComponentEditors;

public interface IComponentEditor
{
    void DrawComponent(Entity entity);
}