namespace Engine.Renderer.Meshes;

public interface IMeshFactory : IDisposable
{
    Mesh CreateCube();
    Mesh Create(string name, IReadOnlyList<Mesh.Vertex> vertices, IReadOnlyList<uint> indices);
}
