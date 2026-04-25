using System.Numerics;
using System.Text.Json.Serialization;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

public class PbrMaterial
{
    [JsonIgnore] public Texture2D? BaseColorTexture { get; set; }

    /// <summary>G = roughness, B = metallic (glTF 2.0 packed MetallicRoughness texture).</summary>
    [JsonIgnore] public Texture2D? MetallicRoughnessTexture { get; set; }

    [JsonIgnore] public Texture2D? NormalTexture { get; set; }
    [JsonIgnore] public Texture2D? AoTexture { get; set; }
    [JsonIgnore] public Texture2D? EmissiveTexture { get; set; }

    public string? BaseColorTexturePath { get; set; }
    public string? MetallicRoughnessTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }
    public string? AoTexturePath { get; set; }
    public string? EmissiveTexturePath { get; set; }

    public Vector4 BaseColorFactor { get; set; } = Vector4.One;
    public float MetallicFactor { get; set; } = 1.0f;
    public float RoughnessFactor { get; set; } = 1.0f;
    public Vector3 EmissiveFactor { get; set; } = Vector3.Zero;
    public float NormalScale { get; set; } = 1.0f;
    public float AoStrength { get; set; } = 1.0f;

    [JsonIgnore] public bool HasBaseColorMap => BaseColorTexture != null;
    [JsonIgnore] public bool HasMetallicRoughnessMap => MetallicRoughnessTexture != null;
    [JsonIgnore] public bool HasNormalMap => NormalTexture != null;
    [JsonIgnore] public bool HasAoMap => AoTexture != null;
    [JsonIgnore] public bool HasEmissiveMap => EmissiveTexture != null;
}
