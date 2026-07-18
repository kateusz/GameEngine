using Engine.Renderer.Textures;

namespace Engine.Renderer;

public sealed class MeshMaterial
{
    public Texture2D? DiffuseTexture { get; set; }
    public Texture2D? SpecularTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }

    public string? DiffuseTexturePath { get; set; }
    public string? SpecularTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }

    public float Shininess { get; set; } = 32.0f;

    public bool HasDiffuseMap => DiffuseTexture != null;
    public bool HasSpecularMap => SpecularTexture != null;
    public bool HasNormalMap => NormalTexture != null;
}
