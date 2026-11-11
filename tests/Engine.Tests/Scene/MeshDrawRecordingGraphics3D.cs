using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Pipeline;

namespace Engine.Tests.Scene;

internal sealed class MeshDrawRecordingGraphics3D : IGraphics3D
{
    private Statistics _stats = new();

    public int MeshDraws { get; private set; }
    public int ShadowQualitySets { get; private set; }
    public SceneView? LastView { get; private set; }
    public int InstanceDrawCalls { get; private set; }
    public int MergedDraws { get; private set; }
    public List<Engine.Renderer.Meshes.Mesh> DrawnMeshes { get; } = [];
    public List<Engine.Renderer.Meshes.Mesh> InstanceMeshes { get; } = [];

    public void DrawMeshInstances(Engine.Renderer.Meshes.Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances)
    {
        MeshDraws++;
        InstanceDrawCalls++;
        InstanceMeshes.Add(mesh);
    }

    public void BeginScene(in SceneView view) => LastView = view;
    public void EndScene() { }
    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1,
        Engine.Renderer.Textures.Texture2D? texture = null, float tilingFactor = 1.0f,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f, Vector3 emissive = default)
    {
        CubeDraws++;
    }
    public int CubeDraws { get; private set; }
    public void DrawMesh(Matrix4x4 transform, Engine.Renderer.Meshes.Mesh mesh, Vector4 tint, int entityId = -1,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f, Vector3 emissive = default)
    {
        MeshDraws++;
        MergedDraws++;
        DrawnMeshes.Add(mesh);
    }
    public void SetAmbientLight(Vector3 color, float strength) { }
    public void SetDirectionalLight(Vector3 direction, Vector3 color) { }
    public void SetPointLights(ReadOnlySpan<PointLightData> lights) { }
    public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled) { }
    public void BeginShadowPass(Matrix4x4 lightViewProjection) { }
    public void BeginShadowCascade(Matrix4x4 lightViewProjection, int cascade) { }
    public void EndShadowPass() { }
    public void SetFarDirectionalShadow(Matrix4x4 lightViewProjection) { }
    public void SetShadowQuality(ShadowQuality quality)
    {
        ShadowQualitySets++;
        ShadowQuality = quality.Sanitized();
    }
    public ShadowQuality ShadowQuality { get; private set; } = new();
    public bool BeginPointShadowFace(int lightIndex, int entityId, int face, Matrix4x4 viewProjection,
        Vector3 lightPosition, float range) => false;
    public void EndPointShadowFace() { }
    public bool UseCachedPointShadow(int lightIndex, int entityId) => false;
    public void SetClearColor(Vector4 color) { }
    public void Clear() { }
    public void ResetStats() => _stats = new Statistics();
    public Engine.Renderer.Statistics GetStats() => _stats;
    public void Init() { }
    public void SetSkybox(string? path) { }
    public void DrawSkybox(Matrix4x4 skyViewProjection) { }
    public void Dispose() { }
}
