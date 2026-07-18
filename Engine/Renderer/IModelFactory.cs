namespace Engine.Renderer;

public interface IModelFactory
{
    Model? Create(string path);
}
