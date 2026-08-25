using System.Numerics;
using Engine.Renderer.Pipeline;
using Serilog;

namespace Engine.Scene.Cameras;

public static class CameraViews
{
    private static readonly ILogger Logger = Log.ForContext(typeof(CameraViews));

    public static SceneView From(Camera camera, Matrix4x4 transform)
    {
        if (!Matrix4x4.Invert(transform, out var view))
        {
            Logger.Error(
                "Failed to invert camera transform matrix (M11={M11}, M22={M22}, M33={M33}, M44={M44}).",
                transform.M11, transform.M22, transform.M33, transform.M44);
            return default;
        }

        return new SceneView(
            view * camera.GetProjectionMatrix(),
            new Vector3(transform.M41, transform.M42, transform.M43));
    }
}
