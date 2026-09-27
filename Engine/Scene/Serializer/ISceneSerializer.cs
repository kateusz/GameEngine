using System.Text.Json.Nodes;

namespace Engine.Scene.Serializer;

public interface ISceneSerializer
{
    void Serialize(IScene scene, string path);
    string SerializeToString(IScene scene);
    void Deserialize(IScene scene, string path);

    /// <summary>
    /// Fills <paramref name="scene"/> from a parsed scene file root (no disk I/O).
    /// Used when JSON was read/parsed off the main thread before ECS deserialization.
    /// </summary>
    void Deserialize(IScene scene, JsonObject jsonRoot);
}
