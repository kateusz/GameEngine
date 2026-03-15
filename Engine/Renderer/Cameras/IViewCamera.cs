using System.Numerics;

namespace Engine.Renderer.Cameras;

/// <summary>
/// Interface for cameras that own their view matrix.
/// Used by viewport tools for coordinate conversion.
/// </summary>
public interface IViewCamera
{
    Matrix4x4 GetViewProjectionMatrix();
    Vector3 GetPosition();
}
