using ECS;

namespace Editor.ComponentEditors.Core;

/// <summary>
/// Interface for the component editor registry that manages component-specific editors.
/// </summary>
public interface IComponentEditorRegistry
{
    /// <summary>
    /// Draws all components of the specified entity using their registered editors.
    /// </summary>
    /// <param name="entity">Entity whose components should be drawn</param>
    void DrawAllComponents(Entity entity);
}
