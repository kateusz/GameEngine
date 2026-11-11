using Engine.Platform.SilkNet;

namespace Engine.Platform.OpenGL;

internal static class OpenGLTextureCaps
{
    private static bool _queried;
    private static bool _s3Tc;

    public static bool SupportsS3Tc
    {
        get
        {
            EnsureQueried();
            return _s3Tc;
        }
    }

    private static void EnsureQueried()
    {
        if (_queried)
            return;

        _queried = true;
        var gl = SilkNetContext.GL;
        _s3Tc = gl.IsExtensionPresent("GL_EXT_texture_compression_s3tc")
                || gl.IsExtensionPresent("EXT_texture_compression_s3tc");
    }

    public static void ResetForTests() => _queried = false;
}
