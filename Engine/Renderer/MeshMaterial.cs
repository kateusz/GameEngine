using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.Renderer;

public sealed class MeshMaterial
{
    public Texture2D? AlbedoTexture { get; set; }
    public Texture2D? MetallicRoughnessTexture { get; set; }
    public Texture2D? NormalTexture { get; set; }
    public Texture2D? EmissiveTexture { get; set; }

    public string? AlbedoTexturePath { get; set; }
    public string? MetallicRoughnessTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }
    public string? EmissiveTexturePath { get; set; }

    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.5f;
    public Vector4 BaseColorFactor { get; set; } = Vector4.One;
    public Vector3 EmissiveFactor { get; set; } = Vector3.Zero;
    public MaterialAlphaMode AlphaMode { get; set; } = MaterialAlphaMode.Opaque;
    public float AlphaCutoff { get; set; } = 0.5f;
    public bool DoubleSided { get; set; }

    public bool HasAlbedoMap => AlbedoTexture != null;
    public bool HasMetallicRoughnessMap => MetallicRoughnessTexture != null;
    public bool HasNormalMap => NormalTexture != null;
    public bool HasEmissiveMap => EmissiveTexture != null;

    public Vector4 ResolveBaseColor(Vector4 tint) => tint * BaseColorFactor;
}
