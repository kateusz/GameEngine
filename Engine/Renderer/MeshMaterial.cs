using Engine.Renderer.Textures;

namespace Engine.Renderer;

public sealed class MeshMaterial
{
    public Texture2D? AlbedoTexture { get; set; }
    public Texture2D? MetallicRoughnessTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }
    public Texture2D? SpecularTexture { get; set; }

    public string? AlbedoTexturePath { get; set; }
    public string? MetallicRoughnessTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }
    public string? SpecularTexturePath { get; set; }

    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.5f;

    public bool HasAlbedoMap => AlbedoTexture != null;
    public bool HasMetallicRoughnessMap => MetallicRoughnessTexture != null;
    public bool HasNormalMap => NormalTexture != null;
    public bool HasSpecularMap => SpecularTexture != null;
}
