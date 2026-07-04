using ECS;

namespace SceneComponents.Rendering;

public class MeshComponent : IComponent
{
    public bool UseBuiltinCube { get; set; }
    public string? ModelPath { get; set; }
    public int? MeshIndex { get; set; }

    public MeshComponent() { }

    public IComponent Clone()
    {
        return new MeshComponent
        {
            UseBuiltinCube = UseBuiltinCube,
            ModelPath = ModelPath,
            MeshIndex = MeshIndex
        };
    }
}
