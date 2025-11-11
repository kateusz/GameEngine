using System.Diagnostics;
using Silk.NET.OpenGL;

namespace Engine.Platform.SilkNet;

/// <summary>
/// OpenGL debug utility for error checking.
/// </summary>
public static class GLDebug
{
    /// <summary>
    /// Checks for OpenGL errors in DEBUG builds only.
    /// This method is compiled out in RELEASE builds for performance.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="operation">Description of the operation that was performed.</param>
    [Conditional("DEBUG")]
    public static void CheckError(GL gl, string operation)
    {
        GLEnum error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            string errorMessage = GetErrorDescription(error);
            Debug.WriteLine($"OpenGL Error after {operation}: {error} (0x{(int)error:X}) - {errorMessage}");
            throw new InvalidOperationException($"OpenGL Error after {operation}: {error} (0x{(int)error:X}) - {errorMessage}");
        }
    }

    /// <summary>
    /// Checks for OpenGL errors in both DEBUG and RELEASE builds.
    /// Use this for critical operations where errors must always be checked.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="operation">Description of the operation that was performed.</param>
    public static void CheckErrorAlways(GL gl, string operation)
    {
        GLEnum error = gl.GetError();
        if (error != GLEnum.NoError)
        {
            string errorMessage = GetErrorDescription(error);
            Debug.WriteLine($"OpenGL Error after {operation}: {error} (0x{(int)error:X}) - {errorMessage}");
            throw new InvalidOperationException($"OpenGL Error after {operation}: {error} (0x{(int)error:X}) - {errorMessage}");
        }
    }

    /// <summary>
    /// Gets a human-readable description of an OpenGL error code.
    /// </summary>
    private static string GetErrorDescription(GLEnum error)
    {
        return error switch
        {
            GLEnum.InvalidEnum => "Invalid enum value passed to OpenGL function",
            GLEnum.InvalidValue => "Invalid numeric value passed to OpenGL function",
            GLEnum.InvalidOperation => "Invalid operation for current OpenGL state",
            GLEnum.StackOverflow => "Stack overflow in OpenGL command",
            GLEnum.StackUnderflow => "Stack underflow in OpenGL command",
            GLEnum.OutOfMemory => "Out of GPU memory",
            GLEnum.InvalidFramebufferOperation => "Framebuffer is not complete",
            _ => "Unknown OpenGL error"
        };
    }
}