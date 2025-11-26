using ECS;

namespace Editor.ComponentEditors.Core;

public interface IComponentEditor
{
    void DrawComponent(Entity entity);
}