using ECS;

namespace Editor.Features.History.Commands;

/// <summary>
/// Removes a component by type; restores from <see cref="IComponent.Clone"/> on Undo.
/// </summary>
public sealed class RemoveComponentCommand(Entity entity, Type componentType) : IUndoCommand
{
    private IComponent? _memento;

    public bool Execute()
    {
        if (!entity.TryGetComponent(componentType, out var component) || component is null)
            return false;

        _memento = component.Clone();
        entity.RemoveComponent(componentType);
        return true;
    }

    public void Undo()
    {
        if (_memento is null)
            return;

        if (!entity.TryGetComponent(componentType, out _))
            entity.AddComponentDynamic(_memento.Clone());
    }
}
