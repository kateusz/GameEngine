using System.Text.Json.Serialization;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

public class MeshMaterial
{
    [JsonIgnore]
    public Texture2D? DiffuseTexture { get; set; }

    [JsonIgnore]
    public Texture2D? SpecularTexture { get; set; }

    [JsonIgnore]
    public Texture2D? NormalTexture { get; set; }

    public float Shininess { get; set; } = 32.0f;

    public string? DiffuseTexturePath { get; set; }
    public string? SpecularTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }

    [JsonIgnore]
    public bool HasDiffuseMap => DiffuseTexture != null;

    [JsonIgnore]
    public bool HasSpecularMap => SpecularTexture != null;

    [JsonIgnore]
    public bool HasNormalMap => NormalTexture != null;
}
