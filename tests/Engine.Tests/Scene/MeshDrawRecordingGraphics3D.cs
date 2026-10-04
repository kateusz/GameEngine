using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Pipeline;

namespace Engine.Tests.Scene;

internal sealed class MeshDrawRecordingGraphics3D : IGraphics3D
{
    public int MeshDraws { get; private set; }

    public void DrawMeshInstances(Engine.Renderer.Meshes.Mesh mesh, ReadOnlySpan<MeshDrawInstance> instances) =>
        MeshDraws++;

    public void BeginScene(in SceneView view) { }
    public void EndScene() { }
    public void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1,
        Engine.Renderer.Textures.Texture2D? texture = null, float tilingFactor = 1.0f,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f, Vector3 emissive = default) { }
    public void DrawMesh(Matrix4x4 transform, Engine.Renderer.Meshes.Mesh mesh, Vector4 tint, int entityId = -1,
        float metallic = 0f, float roughness = 0.5f, float ao = 1f, Vector3 emissive = default) =>
        DrawMeshInstances(mesh, [new MeshDrawInstance
        {
            Transform = transform, EntityId = entityId, Tint = tint, Emissive = emissive
        }]);
    public void SetAmbientLight(Vector3 color, float strength) { }
    public void SetDirectionalLight(Vector3 direction, Vector3 color) { }
    public void SetPointLights(ReadOnlySpan<PointLightData> lights) { }
    public void SetDirectionalShadow(Matrix4x4 lightViewProjection, bool enabled) { }
    public void BeginShadowPass(Matrix4x4 lightViewProjection) { }
    public void EndShadowPass() { }
    public bool BeginPointShadowFace(int lightIndex, int entityId, int face, Matrix4x4 viewProjection,
        Vector3 lightPosition, float range) => false;
    public void EndPointShadowFace() { }
    public bool UseCachedPointShadow(int lightIndex, int entityId) => false;
    public void SetClearColor(Vector4 color) { }
    public void Clear() { }
    public void ResetStats() { }
    public Engine.Renderer.Statistics GetStats() => new();
    public void Init() { }
    public void SetSkybox(string? path) { }
    public void DrawSkybox(Matrix4x4 skyViewProjection) { }
    public void Dispose() { }
}
