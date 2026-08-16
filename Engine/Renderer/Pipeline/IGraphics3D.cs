using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;
using Engine.Scene.Cameras;

namespace Engine.Renderer.Pipeline;

public interface IGraphics3D : IGraphics
{
    void Init();
    void BeginScene(Camera camera, Matrix4x4 transform);
    void BeginScene(IViewCamera camera);
    void EndScene();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? albedo = null, float metallic = 0f, float roughness = 0.5f);
    void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, Vector4 tint, float metallic, float roughness, int entityId = -1, Matrix4x4[]? bonePalette = null, Texture2D? albedoOverride = null);
    void SetWireframe(bool enabled);
    void SetAmbientLight(Vector3 color, float strength);
    void SetDirectionalLight(Vector3 direction, Vector3 color, float strength);
    bool BeginShadowPass();
    void EndShadowPass();
    void SetPointLight(Vector3 position, Vector3 color, float strength, float range);
    bool BeginPointShadowPass();
    void SetPointShadowFace(int face);
    void EndPointShadowPass();
    void BeginTransparentPass();
    void EndTransparentPass();
    void SetEnvironment(string? resolvedHdrPath, float intensity);
    void DrawBuiltinSphere(Matrix4x4 transform, Vector4 tint, float metallic, float roughness, int entityId = -1, Texture2D? albedo = null);
    void DrawSkybox();
    void ResetStats();
    Statistics GetStats();
}
