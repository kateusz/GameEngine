using ECS;
using SceneComponents;

namespace Editor.Features.History.Commands;

/// <summary>
/// Adds a component; optionally auto-adds Transform in the same undo step.
/// </summary>
public sealed class AddComponentCommand(
    Entity entity,
    IComponent component,
    bool autoAddTransform = false) : IUndoCommand
{
    private readonly Type _componentType = component.GetType();
    private bool _addedComponent;
    private bool _addedTransform;

    public bool Execute()
    {
        _addedComponent = false;
        _addedTransform = false;

        if (autoAddTransform && !entity.HasComponent<TransformComponent>())
        {
            entity.AddComponent<TransformComponent>();
            _addedTransform = true;
        }

        if (!entity.TryGetComponent(_componentType, out _))
        {
            entity.AddComponentDynamic(component.Clone());
            _addedComponent = true;
        }

        return _addedComponent || _addedTransform;
    }

    public void Undo()
    {
        if (_addedComponent)
            entity.RemoveComponent(_componentType);
        if (_addedTransform)
            entity.RemoveComponent<TransformComponent>();
    }
}
