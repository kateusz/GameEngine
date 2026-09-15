namespace Engine.Renderer.Models;

public interface IModelFactory : IDisposable
{
    Model? Create(string path);
    void Clear();
}