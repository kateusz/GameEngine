using ECS;
using Engine.Scene;
using SceneComponents;
using SceneComponents.Camera;

namespace Editor.Features.History.Commands;

/// <summary>
/// In-memory Clone-bag of a destroyed subtree. JSON fallback not needed —
/// NativeScriptComponent.Clone preserves ScriptTypeName.
/// </summary>
internal sealed class EntitySubtreeSnapshot
{
    private readonly IReadOnlyList<EntityBagEntry> _entries;

    private EntitySubtreeSnapshot(IReadOnlyList<EntityBagEntry> entries)
    {
        _entries = entries;
    }

    public static EntitySubtreeSnapshot Capture(IScene scene, Entity root)
    {
        var subtree = scene.CollectSubtree(root);
        var entries = new List<EntityBagEntry>(subtree.Count);

        foreach (var entity in subtree)
        {
            var components = entity.GetAllComponents()
                .Select(c => c.Clone())
                .ToList();
            entries.Add(new EntityBagEntry(entity.Id, entity.Name, components));
        }

        return new EntitySubtreeSnapshot(entries);
    }

    /// <summary>Recreates entities with new IDs; returns remapped root id.</summary>
    public int Restore(IScene scene)
    {
        var idMap = new Dictionary<int, int>(_entries.Count);
        Entity? rootClone = null;

        foreach (var entry in _entries)
        {
            var clone = scene.CreateEntity(entry.Name);
            idMap[entry.OldId] = clone.Id;

            foreach (var component in entry.Components)
                clone.AddComponentDynamic(component.Clone());

            rootClone ??= clone;
        }

        foreach (var (_, newId) in idMap)
        {
            var clone = scene.Context.GetById(newId);
            if (!clone.TryGetComponent<ParentComponent>(out var parentComp) || parentComp.ParentId is not int oldParentId)
                continue;

            if (idMap.TryGetValue(oldParentId, out var mappedParentId))
                parentComp.ParentId = mappedParentId;
            // else keep ParentId — restored root stays under the same external parent
        }

        scene.RebuildHierarchyIndex();

        if (rootClone!.HasComponent<CameraComponent>() && rootClone.GetComponent<CameraComponent>().Primary)
            scene.SetPrimaryCamera(rootClone);

        return rootClone.Id;
    }

    private sealed record EntityBagEntry(int OldId, string Name, IReadOnlyList<IComponent> Components);
}
