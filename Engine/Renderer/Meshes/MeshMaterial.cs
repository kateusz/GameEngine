using Engine.Renderer.Textures;

namespace Engine.Renderer.Meshes;

public readonly record struct MeshMaterial(
    Texture2D? Diffuse,
    Texture2D? Specular,
    Texture2D? Normal,
    float Shininess = 32.0f)
{
    public static MeshMaterial Default { get; } = new(null, null, null);

    public bool HasDiffuseMap => Diffuse != null;
    public bool HasSpecularMap => Specular != null;
    public bool HasNormalMap => Normal != null;

    public MeshMaterial WithDiffuse(Texture2D? diffuse) => this with { Diffuse = diffuse };
}
