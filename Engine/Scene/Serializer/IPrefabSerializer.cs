using ECS;

namespace Engine.Scene.Serializer;

public interface IPrefabSerializer
{
    /// <summary>
    /// Serialize an entity (and its descendants) to a prefab file (always v2 format).
    /// </summary>
    void SerializeToPrefab(IScene scene, Entity entity, string prefabName, string projectPath);

    /// <summary>
    /// Apply prefab data to an existing entity. v1 replaces components on the entity;
    /// v2 replaces the subtree rooted at the entity.
    /// </summary>
    void ApplyPrefabToEntity(IScene scene, Entity entity, string prefabPath);

    /// <summary>
    /// Instantiate a prefab into the scene. Returns the root entity.
    /// </summary>
    Entity CreateEntityFromPrefab(IScene scene, string prefabPath, string? entityName = null);
}
