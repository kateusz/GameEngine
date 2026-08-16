using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    public string? ModelPath { get; set; }
    public string? AlbedoTexturePath { get; set; }
    public Vector4 Color { get; set; } = Vector4.One;
    public float? MetallicOverride { get; set; }
    public float? RoughnessOverride { get; set; }

    [JsonIgnore]
    public Matrix4x4[]? BonePalette { get; set; }

    [JsonIgnore]
    public Matrix4x4 SkinningWorld { get; set; }

    /// <summary>First submesh index to draw. Used with <see cref="SubmeshCount"/>.</summary>
    public int SubmeshStart { get; set; }

    /// <summary>
    /// Number of submeshes to draw from <see cref="SubmeshStart"/>.
    /// <c>-1</c> (default) = draw all submeshes in the file.
    /// </summary>
    public int SubmeshCount { get; set; } = -1;

    public ModelRendererComponent() { }

    public ModelRendererComponent(Vector4 color)
    {
        Color = color;
    }

    public IComponent Clone() => new ModelRendererComponent
    {
        ModelPath = ModelPath,
        AlbedoTexturePath = AlbedoTexturePath,
        Color = Color,
        MetallicOverride = MetallicOverride,
        RoughnessOverride = RoughnessOverride,
        SubmeshStart = SubmeshStart,
        SubmeshCount = SubmeshCount
    };
}
