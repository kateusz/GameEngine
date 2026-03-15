using System.Numerics;
using Engine.Renderer.Cameras;

namespace Engine.Scene.Systems;

internal interface IPrimaryCameraProvider
{
    Camera? Camera { get; }
    Matrix4x4 Transform { get; }
}
