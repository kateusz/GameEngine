using System.Diagnostics;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

/// <summary>
/// OpenGL debug utility for error checking in DEBUG builds.
/// </summary>
public static class GLDebug
{
    [Conditional("DEBUG")]
    public static void CheckError(GL gl, string operation)
    {
        GLEnum error;
        while ((error = gl.GetError()) != GLEnum.NoError)
        {
            Debug.WriteLine($"OpenGL Error after {operation}: {error} (0x{(int)error:X})");
            throw new InvalidOperationException($"OpenGL Error after {operation}: {error} (0x{(int)error:X})");
        }
    }
}