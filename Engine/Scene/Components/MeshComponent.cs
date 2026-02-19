using System.Text.Json.Serialization;
using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class MeshComponent : IComponent
{
    public string MeshPath { get; set; } = string.Empty;

    [JsonIgnore]
    public Mesh? Mesh { get; set; }

    public MeshComponent()
    {
        // Mesh must be set externally via SetMesh() or property setter
    }

    public MeshComponent(Mesh mesh)
    {
        Mesh = mesh;
    }

    public MeshComponent(string meshPath)
    {
        MeshPath = meshPath;
    }

    public void SetMesh(Mesh mesh) => Mesh = mesh;

    public IComponent Clone()
    {
        return new MeshComponent
        {
            MeshPath = MeshPath,
            Mesh = Mesh
        };
    }
}
