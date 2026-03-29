using System.Text.Json.Serialization;
using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class MeshComponent : IComponent
{
    public string? ModelPath { get; set; }
    public int? MeshIndex { get; set; }

    [JsonIgnore]
    public List<Mesh> Meshes { get; set; } = [];

    [JsonIgnore]
    public int MeshCount => Meshes.Count;

    public MeshComponent() { }

    public MeshComponent(List<Mesh> meshes, string? modelPath = null)
    {
        Meshes = meshes;
        ModelPath = modelPath;
    }

    public void SetModel(List<Mesh> meshes, string? modelPath = null)
    {
        Meshes = meshes;
        ModelPath = modelPath;
    }

    public void SetMesh(Mesh mesh)
    {
        Meshes = [mesh];
    }

    public IComponent Clone()
    {
        return new MeshComponent
        {
            Meshes = Meshes,
            ModelPath = ModelPath,
            MeshIndex = MeshIndex
        };
    }
}
