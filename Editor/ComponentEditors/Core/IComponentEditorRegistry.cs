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
    void DrawAllComponents(Entity entity);

    /// <summary>
    /// Draws components shared by every entity in the selection.
    /// </summary>
    void DrawCommonComponents(IReadOnlyList<Entity> entities);
}
