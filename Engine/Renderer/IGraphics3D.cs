using System.Numerics;
using Engine.Renderer.Cameras;
using Engine.Scene.Components;

namespace Engine.Renderer;

public interface IGraphics3D : IGraphics
{
    void Init();
    void BeginScene(Camera camera, Matrix4x4 transform);
    void EndScene();
    void DrawMesh(Matrix4x4 transform, Mesh mesh, Vector4 color, int entityId = -1);
    void DrawModel(Matrix4x4 transform, MeshComponent meshComponent, ModelRendererComponent modelRenderer, int entityId = -1);
    void SetLightPosition(Vector3 position);
    void SetLightColor(Vector3 color);
    void SetShininess(float shininess);
    void ResetStats();
    Statistics GetStats();
}