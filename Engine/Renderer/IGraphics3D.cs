using System.Numerics;
using Engine.Scene.Cameras;

namespace Engine.Renderer;

public interface IGraphics3D : IGraphics
{
    void Init();
    void BeginScene(Camera camera, Matrix4x4 transform);
    void BeginScene(IViewCamera camera);
    void EndScene();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1);
    void DrawMesh(Matrix4x4 transform, Mesh mesh, MeshMaterial material, Vector4 tint, float metallic, float roughness, int entityId = -1);
    void SetWireframe(bool enabled);
    void SetAmbientLight(Vector3 color, float strength);
    void SetDirectionalLight(Vector3 direction, Vector3 color);
    void ResetStats();
    Statistics GetStats();
}
