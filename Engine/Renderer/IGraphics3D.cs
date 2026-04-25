using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Renderer;

public interface IGraphics3D : IGraphics
{
    void Init();
    void BeginScene(Camera camera, Matrix4x4 transform);
    void BeginScene(IViewCamera camera);
    void EndScene();
    void DrawMesh(Matrix4x4 transform, Mesh mesh, PbrMaterial material, int entityId = -1);
    void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1);
    void SetDirectionalLight(bool enabled, Vector3 direction, Vector3 color, float strength);
    void SetAmbientLight(bool enabled, Vector3 color, float strength);
    void SetPointLights(IReadOnlyList<PointLightData> pointLights);

    void BeginLightVisualization(Camera camera, Matrix4x4 transform);
    void BeginLightVisualization(IViewCamera camera);
    void DrawLightVisualization(Vector3 position, float scale = 0.5f);
    void EndLightVisualization();

    void ResetStats();
    Statistics GetStats();
}
