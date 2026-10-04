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

    /// <summary>
    /// Mesh-space center baked into <see cref="TransformComponent"/> so the entity sits on the geometry.
    /// Drawing subtracts it so the mesh does not move. Zero for older imports.
    /// </summary>
    public Vector3 Pivot { get; set; }
    /// <summary>Skip drawing this renderer; children draw the unpacked submeshes instead.</summary>
    public bool SuppressDraw { get; set; }

    /// <summary>Submesh metallic/roughness were copied onto this renderer. Stops a later draw from overwriting an edit.</summary>
    public bool FactorsSeeded { get; set; }

    public float Metallic
    {
        get;
        set => field = Sanitize(value, 0f);
    }

    public float Roughness
    {
        get;
        set => field = Sanitize(value, 0.5f);
    } = 0.5f;

    public float Ao
    {
        get;
        set => field = Sanitize(value, 1f);
    } = 1f;

    /// <summary>Linear emissive color, 0–1. Multiplied by <see cref="EmissiveStrength"/>.</summary>
    public Vector3 Emissive
    {
        get;
        set => field = new Vector3(Sanitize(value.X, 0f), Sanitize(value.Y, 0f), Sanitize(value.Z, 0f));
    }

    /// <summary>Scales <see cref="Emissive"/>. Values above 1 are what push a surface over the bloom threshold.</summary>
    public float EmissiveStrength
    {
        get;
        set => field = float.IsFinite(value) ? MathF.Max(0f, value) : 0f;
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
        FactorsSeeded = FactorsSeeded,
        Metallic = Metallic,
        Roughness = Roughness,
        Ao = Ao,
        Emissive = Emissive,
        EmissiveStrength = EmissiveStrength,
        VisibilityZoneEntityId = VisibilityZoneEntityId
    };
}