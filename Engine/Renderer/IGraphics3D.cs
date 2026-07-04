using System.Numerics;
using Engine.Renderer.Cameras;

namespace Engine.Renderer;

public interface IGraphics3D : IGraphics
{
    void Init();
    void BeginScene(Camera camera, Matrix4x4 transform);
    void BeginScene(IViewCamera camera);
    void EndScene();
    void DrawCube(Matrix4x4 transform, Vector4 color, int entityId = -1);
    void ResetStats();
    Statistics GetStats();
}
