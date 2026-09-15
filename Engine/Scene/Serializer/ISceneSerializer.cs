namespace Engine.Scene.Serializer;

public interface ISceneSerializer
{
    void Serialize(IScene scene, string path);
    string SerializeToString(IScene scene);
    void Deserialize(IScene scene, string path);
}
