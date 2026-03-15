using System.Text.Json.Serialization;
using ECS;
using Engine.Renderer;

namespace Engine.Scene.Components;

public class MeshComponent : IComponent
{
    /// <summary>
    /// Relative path to the mesh asset file (e.g. "assets/objModels/person.model").
    /// Persisted to JSON; used by the runtime to load the mesh via IMeshFactory.
    /// Null indicates a procedurally generated mesh or one with no known asset path.
    /// </summary>
    public string? MeshPath { get; set; }

    /// <summary>
    /// Loaded GPU mesh resource. Not serialized — set at runtime by the mesh loading system.
    /// </summary>
    [JsonIgnore]
    public Mesh? Mesh { get; set; }

    public MeshComponent()
    {
    }

    public MeshComponent(Mesh mesh, string? meshPath = null)
    {
        Mesh = mesh;
        MeshPath = meshPath;
    }

    public void SetMesh(Mesh mesh, string? meshPath = null)
    {
        Mesh = mesh;
        MeshPath = meshPath;
    }

    public IComponent Clone()
    {
        return new MeshComponent
        {
            Mesh = Mesh,
            MeshPath = MeshPath
        };
    }
}