using ECS;

namespace Editor.UI.Elements;

public interface IPrefabManager
{
    void ShowSavePrefabPopup(Entity entity);
    void RenderPopups();
}