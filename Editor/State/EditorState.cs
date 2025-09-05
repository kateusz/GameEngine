using ECS;

namespace Editor.State;

public class EditorState
{
    public Entity? HoveredEntity { get; set; }
    public Entity? SelectedEntity { get; private set; }
    
    public event Action<Entity?> SelectionChanged = delegate { };
    
    public void SelectEntity(Entity? entity)
    {
        if (Equals(SelectedEntity, entity)) 
            return;
        
        SelectedEntity = entity;
        SelectionChanged(SelectedEntity);
    }
}