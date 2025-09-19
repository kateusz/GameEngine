namespace Engine.Scene.Serializer;

public interface ISceneSerializer
{
    void Serialize(Scene scene, string path);
    void Deserialize(Scene scene, string path);
}