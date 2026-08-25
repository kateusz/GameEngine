namespace Engine.Renderer.Models;

public interface IModelFactory
{
    Model? Create(string path, bool mergeByMaterial = false);
    void Dispose();
}