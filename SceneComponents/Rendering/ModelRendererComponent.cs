using System.Numerics;
using ECS;

namespace SceneComponents.Rendering;

public class ModelRendererComponent : IComponent
{
    private float _metallic;
    private float _roughness = 0.5f;
    private float _ao = 1f;

    public Vector4 Color { get; set; } = Vector4.One;
    public string? TexturePath { get; set; }
    public float TilingFactor { get; set; } = 1.0f;
    public string? ModelPath { get; set; }
    /// <summary>When set, only this submesh index from the model file is drawn. Used by hierarchy unpack.</summary>
    public int? MeshIndex { get; set; }
    /// <summary>Skip drawing this renderer; children draw the unpacked submeshes instead.</summary>
    public bool SuppressDraw { get; set; }

    public float Metallic
    {
        get => _metallic;
        set => _metallic = Sanitize(value, 0f);
    }

    public float Roughness
    {
        get => _roughness;
        set => _roughness = Sanitize(value, 0.5f);
    }

    public float Ao
    {
        get => _ao;
        set => _ao = Sanitize(value, 1f);
    }

    /// <summary>When true, metallic, roughness, and AO replace the imported factors. Cubes ignore this.</summary>
    public bool OverrideMaterial { get; set; }

    private static float Sanitize(float value, float fallback) =>
        float.IsFinite(value) ? System.Math.Clamp(value, 0f, 1f) : fallback;

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
        Metallic = Metallic,
        Roughness = Roughness,
        Ao = Ao,
        OverrideMaterial = OverrideMaterial
    };
}