namespace Engine.Renderer.Textures;

public interface ISkyCapture : IDisposable
{
    bool BeginFace(int face);
}