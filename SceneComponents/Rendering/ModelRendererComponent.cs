using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    public Vector4 Color { get; set; } = Vector4.One;
    public string? TexturePath { get; set; }
    public float TilingFactor { get; set; } = 1.0f;
    public string? ModelPath { get; set; }
    /// <summary>When set, only this submesh index from the model file is drawn. Used by hierarchy unpack.</summary>
    public int? MeshIndex { get; set; }
    /// <summary>Skip drawing this renderer; children draw the unpacked submeshes instead.</summary>
    public bool SuppressDraw { get; set; }
    /// <summary>When true, load a material-merged variant of the model (fewer draw calls, no hierarchy unpack).</summary>
    public bool MergeByMaterial { get; set; }

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone() => new ModelRendererComponent
    {
        Color = Color,
        TexturePath = TexturePath,
        TilingFactor = TilingFactor,
        ModelPath = ModelPath,
        MeshIndex = MeshIndex,
        SuppressDraw = SuppressDraw,
        MergeByMaterial = MergeByMaterial
    };
}