using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class MeshComponent : IComponent
{
    public Mesh Mesh { get; set; }
    
    public MeshComponent()
    {
        // Create a default cube mesh
        Mesh = MeshFactory.CreateCube();
    }
    
    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }

    public void SetMesh(Mesh mesh) => Mesh = mesh;

    public IComponent Clone()
    {
        // Share the same Mesh reference (meshes are typically immutable resources)
        return new MeshComponent(Mesh);
    }
}