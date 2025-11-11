namespace Engine.Scene.Serializer;

public interface ISceneSerializer
{
    void Serialize(IScene scene, string path);
    void Deserialize(IScene scene, string path);
}