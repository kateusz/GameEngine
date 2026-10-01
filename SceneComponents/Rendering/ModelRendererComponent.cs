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

    /// <summary>
    /// Mesh-space center baked into <see cref="TransformComponent"/> so the entity sits on the geometry.
    /// Drawing subtracts it so the mesh does not move. Zero for older imports.
    /// </summary>
    public Vector3 Pivot { get; set; }
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

    /// <summary>Zone entity id for visibility filtering, or -1 to use frustum only.</summary>
    public int VisibilityZoneEntityId { get; set; } = -1;

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
        Pivot = Pivot,
        SuppressDraw = SuppressDraw,
        Metallic = Metallic,
        Roughness = Roughness,
        Ao = Ao,
        VisibilityZoneEntityId = VisibilityZoneEntityId
    };
}