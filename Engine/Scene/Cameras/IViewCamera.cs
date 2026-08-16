using System.Numerics;

namespace Engine.Scene.Cameras;

/// <summary>
/// Interface for cameras that own their view matrix.
/// Used by viewport tools for coordinate conversion.
/// </summary>
public interface IViewCamera
{
    Matrix4x4 GetViewProjectionMatrix();
    Matrix4x4 GetViewMatrix();
    Matrix4x4 GetProjectionMatrix();
    Vector3 GetPosition();
}
