using System.Numerics;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Materials;

/// <summary>
/// PBR material using the metallic/roughness workflow.
/// Holds texture references and scalar fallback values.
/// </summary>
public class PBRMaterial : IDisposable
{
    public Texture2D? AlbedoMap { get; set; }
    public Texture2D? NormalMap { get; set; }
    public Texture2D? MetallicMap { get; set; }
    public Texture2D? RoughnessMap { get; set; }
    public Texture2D? AmbientOcclusionMap { get; set; }
    public Texture2D? EmissiveMap { get; set; }

    public Vector4 AlbedoColor { get; set; } = Vector4.One;
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.5f;
    public float AmbientOcclusion { get; set; } = 1.0f;
    public Vector3 EmissiveColor { get; set; } = Vector3.Zero;
    public float EmissiveIntensity { get; set; }

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
            return;

        // Materials don't own textures - factories do
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
