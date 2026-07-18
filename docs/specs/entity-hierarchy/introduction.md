# Entity Hierarchy — Introduction

## Problem

The engine today treats every entity as an independent object in a flat list. Each entity has a local transform, but that transform is used directly as if it were a world-space position — there is no way to express that one entity should move with another. A weapon on a character, a wheel on a vehicle, or a grouped set of level props must all be positioned manually and updated every frame if the parent moves.

The Scene Hierarchy panel in the editor reinforces the illusion of a tree (expand/collapse icons) but lists entities in a flat, searchable array. Prefabs are single-entity component blobs. Deleting, duplicating, or saving a composite object has no concept of "this group belongs together."

Entity hierarchy solves the problem of **composite objects**: entities that have a logical and spatial relationship where children inherit their parent's movement, rotation, and scale.

## What this feature delivers

- **Parent-child relationships** between entities, stored as data and editable in the editor.
- **Transform inheritance** — a child's world position is derived from its parent's world transform multiplied by its own local transform.
- **Nested Scene Hierarchy** — the editor panel shows a real tree rooted at scene-level entities, with drag-and-drop reparenting.
- **Cascade lifecycle** — deleting a parent destroys all descendants; duplicating a parent clones the entire subtree.
- **Multi-entity prefabs** — save and instantiate an entire parent-plus-children subtree as one `.prefab` file.
- **Runtime script API** — game scripts can query and change parent-child links at runtime.
- **Serialization** — parent references survive scene save/load and prefab round-trips.

## What this feature explicitly does not do (v1)

- **Physics parenting** — `Rigidbody2D` bodies are not repositioned from hierarchy. Physics continues to use its existing local transform as today. Physics-aware parenting is a follow-up.
- **World-space gizmo toggle** — Move/Rotate/Scale tools edit local transform relative to the parent. A local/world mode switch is deferred.
- **Undo/redo for reparent** — hierarchy edits are immediate; undo integration is out of scope.
- **Multi-select or batch reparent** — single-entity selection and reparent only.

## Key terminology

**Entity.** The fundamental object in the ECS. Identified by a unique integer `Id`. Carries components; has no implicit spatial meaning beyond what its components provide.

**Parent.** The entity one level above another in the hierarchy. An entity has at most one parent. The scene root has no parent.

**Child.** An entity whose parent is another entity. An entity may have zero or many children.

**Root entity.** An entity with no parent. Root entities appear at the top level of the Scene Hierarchy panel.

**Descendant.** Any child, grandchild, or deeper relative of an entity. The full subtree below a node.

**Ancestor.** Any parent, grandparent, or deeper relative above an entity.

**Local transform.** Translation, rotation, and scale relative to the parent entity's coordinate frame. Stored on `TransformComponent` as today. If there is no parent, local transform equals world transform.

**World transform.** The absolute position, rotation, and scale of an entity in scene space. Computed each frame (or on demand when dirty) by composing local transforms up the ancestor chain: `world = parentWorld × local`.

**Reparent.** Change an entity's parent — attach it under a different node or detach it to root. Must never create a cycle (an entity cannot become its own ancestor).

**Subtree.** An entity plus all its descendants. Operations like duplicate, delete, and prefab save/load work on entire subtrees.

**Prefab subtree.** A multi-entity prefab file containing several entities with internal parent references. Instantiating the prefab creates the full tree in the scene.

**Children index.** A scene-maintained lookup from parent entity Id to ordered child Ids. Not serialized separately; rebuilt from `ParentComponent` data on load. Enables fast tree rendering without scanning all entities.

**Dirty propagation.** When a local transform or parent link changes, the entity and all descendants are marked so their world transforms are recomputed.

## Patterns and principles

**Single source of truth for parent links.** The parent relationship is stored once — on the child, via a `ParentComponent` holding the parent's entity Id (or null for roots). The children index is always derived from this data, never authored independently. This prevents the classic bug of parent and child lists disagreeing after a load or clone.

**Local storage, world consumption.** Gameplay and rendering code store local transforms (what the artist edits) but consume world transforms (where things actually appear). The conversion happens in one dedicated pass early in the frame, before rendering and audio systems run.

**Hierarchy operations go through the scene.** Reparent, cascade delete, and subtree duplicate are scene-level APIs — not ad-hoc component edits scattered across the editor and scripts. Centralizing these operations guarantees the children index stays consistent, cycles are rejected, and lifecycle hooks (`OnDestroy` on scripts) fire in a defined order.

**Depth-first lifecycle.** When destroying a subtree, children are destroyed before parents (deepest first) so script `OnDestroy` handlers see a consistent world. When duplicating, entities are created parent-before-child so parent Ids exist when children reference them.

**Prefab-local identity.** Inside a prefab file, entities reference each other by prefab-local index (position in the entity array), not scene Ids. On instantiation, a remapping table translates prefab-local indices to newly assigned scene Ids.

**Fail on invalid hierarchy.** Loading a scene with a `ParentId` pointing to a missing entity detaches the orphan to root and logs a warning. Attempting to reparent into a cycle throws or returns failure — never silently corrupts the tree.

## Architecture philosophy

**Minimal new surface area.** One new component (`ParentComponent`), one new system (`TransformHierarchySystem`), and hierarchy methods on the existing scene interface. No parallel entity graph, no bidirectional serialized child lists, no changes to the core `Entity` class shape.

**Follow existing ECS conventions.** The new component implements `IComponent` and `Clone()` like every other scene component. It registers in `ComponentSerializerRegistry` for JSON round-trip. Editor gets a component editor only if useful (parent picker); the primary UX is drag-reparent in the hierarchy panel.

**Incremental correctness over feature breadth.** Physics integration, world-space gizmos, and undo are real needs but add complexity that does not block the core value of composite transforms. Ship the transform + editor + serialization + script path first; extend consumers in follow-up work.

**Lazy senior defaults.** Local-space gizmo editing matches what is stored and avoids inverse-matrix math in the viewport tools for v1. Cascade delete matches user expectation from Unity-like editors. Multi-entity prefabs use the same entity array format as scenes, minimizing a second serialization dialect.
