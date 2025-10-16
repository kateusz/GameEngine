using ECS;

namespace Engine.Scene.Serializer;

public interface IPrefabSerializer
{
    /// <summary>
    /// Serialize an entity to a prefab file
    /// </summary>
    /// <param name="entity">The entity to serialize</param>
    /// <param name="prefabName">Name of the prefab file (without extension)</param>
    /// <param name="projectPath">Path to the project root</param>
    void SerializeToPrefab(Entity entity, string prefabName, string projectPath);

    /// <summary>
    /// Apply prefab data to an existing entity (replaces all components)
    /// </summary>
    /// <param name="entity">The entity to apply prefab to</param>
    /// <param name="prefabPath">Path to the prefab file</param>
    void ApplyPrefabToEntity(Entity entity, string prefabPath);

    /// <summary>
    /// Create a new entity from a prefab
    /// </summary>
    /// <param name="prefabPath">Path to the prefab file</param>
    /// <param name="entityName">Name for the new entity</param>
    /// <param name="entityId">ID for the new entity</param>
    /// <returns>New entity with prefab components</returns>
    Entity CreateEntityFromPrefab(string prefabPath, string entityName, Guid entityId);
}