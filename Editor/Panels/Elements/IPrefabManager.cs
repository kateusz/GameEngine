using ECS;

namespace Editor.Panels.Elements;

public interface IPrefabManager
{
    void ShowSavePrefabDialog(Entity entity);
    void RenderPopups();
}