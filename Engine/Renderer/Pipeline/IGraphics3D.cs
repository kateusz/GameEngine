using System.Numerics;
using Engine.Scene.Cameras;

namespace Engine.Renderer.Pipeline;

public interface IGraphics3D : IGraphics
{
    void BeginScene(Camera camera, Matrix4x4 transform);
    void BeginScene(IViewCamera camera);
    void EndScene();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1);
    void SetAmbientLight(Vector3 color, float strength);
    void SetDirectionalLight(Vector3 direction, Vector3 color);
    Statistics GetStats();
}