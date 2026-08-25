using System.Numerics;
using Engine.Renderer.Meshes;
using Engine.Renderer.Textures;

namespace Engine.Renderer.Pipeline;

public interface IGraphics3D : IGraphics
{
    void BeginScene(in SceneView view);
    void EndScene();
    bool BeginShadowPass(Matrix4x4 lightSpaceMatrix);
    void EndShadowPass();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1, Texture2D? texture = null,
        float tilingFactor = 1.0f);
    void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 tint, int entityId = -1);
    void SetAmbientLight(Vector3 color, float strength);
    void SetDirectionalLight(Vector3 direction, Vector3 color, Matrix4x4? lightSpaceMatrix = null);
    void DrawSkybox(Texture2D hdrTexture, float intensity, float yawRadians);
    void ResetStats();
    Statistics GetStats();
}
