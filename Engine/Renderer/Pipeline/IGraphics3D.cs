using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Pipeline;

public interface IGraphics3D : IGraphics
{
    void BeginScene(in SceneView view);
    void EndScene();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f, float metallic = 0f, float roughness = 0.5f, float ao = 1f);
    void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f);

    /// <summary>
    /// One draw for every entry. Callers group by mesh and by tint / metallic / roughness / AO.
    /// </summary>
    void DrawMeshInstances(Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances);
    void SetAmbientLight(Vector3 color, float strength);
    void SetDirectionalLight(Vector3 direction, Vector3 color);
    void SetPointLights(ReadOnlySpan<PointLightData> lights);
    void BeginShadowPass(Matrix4x4 lightViewProjection);
    void EndShadowPass();
    void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled);
    bool BeginPointShadowFace(
        int lightIndex, int entityId, int face,
        Matrix4x4 viewProjection, Vector3 lightPosition, float range);
    bool UseCachedPointShadow(int lightIndex, int entityId);
    void EndPointShadowFace();
    void ResetStats();
    Statistics GetStats();
}

public readonly struct MeshDrawInstance
{
    public Matrix4x4 Transform { get; init; }
    public int EntityId { get; init; }
    public Vector4 Tint { get; init; }
    public float Metallic { get; init; }
    public float Roughness { get; init; }
    public float Ao { get; init; }
}
