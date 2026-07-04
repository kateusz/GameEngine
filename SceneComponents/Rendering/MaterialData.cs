namespace SceneComponents.Rendering;

public class MaterialData
{
    public float Shininess { get; set; } = 32.0f;
    public string? DiffuseTexturePath { get; set; }
    public string? SpecularTexturePath { get; set; }
    public string? NormalTexturePath { get; set; }
}
