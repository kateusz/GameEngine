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
    void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 color, int entityId = -1);
    void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1);
    void SetLightPosition(Vector3 position);
    void SetLightColor(Vector3 color);
    void SetShininess(float shininess);
    
    void BeginLightVisualization(Camera camera, Matrix4x4 transform);
    void BeginLightVisualization(IViewCamera camera);
    void DrawLightVisualization(Vector3 position, float scale = 0.5f);
    void EndLightVisualization();
    
    void ResetStats();
    Statistics GetStats();
}