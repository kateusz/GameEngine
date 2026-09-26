using ECS;
using Engine.Renderer.Models;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Rendering;

namespace Editor.Features.History.Commands;

/// <summary>
/// Creates a new scene entity and assigns the dropped model, including hierarchy unpack.
/// Undo removes the entity and any unpacked children.
/// </summary>
public sealed class SpawnModelEntityCommand(
    IScene scene,
    string entityName,
    Model model,
    string relativeModelPath) : IUndoCommand
{
    public int? EntityId { get; private set; }

    public bool Execute()
    {
        var root = scene.CreateEntity(entityName);
        root.AddComponent<TransformComponent>();
        var component = new ModelRendererComponent();
        root.AddComponent(component);
        EntityId = root.Id;

        new ImportModelHierarchyCommand(scene, root, component, model, relativeModelPath).Execute();
        return true;
    }

    public void Undo()
    {
        if (EntityId is not int id || !scene.Context.Contains(id))
            return;

        scene.DestroyEntity(scene.Context.GetById(id));
    }
}
