namespace Engine.Scene.Serializer;

/// <summary>
/// Interface for serializing and deserializing scenes to/from JSON.
/// Supports both file-based and string-based serialization for snapshots.
/// </summary>
public interface ISceneSerializer
{
    /// <summary>
    /// Serializes a scene to a JSON file at the specified path.
    /// </summary>
    void Serialize(IScene scene, string path);

    /// <summary>
    /// Deserializes a scene from a JSON file at the specified path.
    /// </summary>
    void Deserialize(IScene scene, string path);

    /// <summary>
    /// Serializes the scene to a JSON string (for in-memory snapshots).
    /// Used for capturing scene state before entering play mode.
    /// </summary>
    /// <param name="scene">The scene to serialize</param>
    /// <returns>JSON string representation of the scene</returns>
    string SerializeToString(IScene scene);

    /// <summary>
    /// Deserializes the scene from a JSON string (for snapshot restoration).
    /// Used for restoring scene state when stopping or restarting play mode.
    /// </summary>
    /// <param name="scene">The scene to populate with deserialized entities</param>
    /// <param name="json">JSON string containing the serialized scene</param>
    void DeserializeFromString(IScene scene, string json);
}