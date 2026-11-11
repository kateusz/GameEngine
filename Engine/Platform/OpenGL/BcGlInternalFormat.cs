using Engine.Platform.SilkNet;
using Engine.Renderer.Textures.Compression;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;

namespace Engine.Platform.OpenGL;

internal static class BcGlInternalFormat
{
    private static bool? _s3Tc;

    public static bool SupportsS3Tc =>
        _s3Tc ??= SilkNetContext.GL.IsExtensionPresent("GL_EXT_texture_compression_s3tc")
                  || SilkNetContext.GL.IsExtensionPresent("EXT_texture_compression_s3tc");

    public static bool TryGet(BcGpuFormat format, out InternalFormat internalFormat)
    {
        if (BcFormatPick.UsesS3Tc(format) && !SupportsS3Tc)
        {
            internalFormat = default;
            return false;
        }

        internalFormat = format switch
        {
            BcGpuFormat.Dxt1Srgb => (InternalFormat)0x8C4C, // COMPRESSED_SRGB_S3TC_DXT1_EXT
            BcGpuFormat.Dxt5Srgb => (InternalFormat)0x8C4F, // COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT
            BcGpuFormat.Dxt5Linear => (InternalFormat)0x83F3, // COMPRESSED_RGBA_S3TC_DXT5_EXT
            BcGpuFormat.Bc4 => InternalFormat.CompressedRedRgtc1,
            BcGpuFormat.Bc5 => (InternalFormat)0x8DBD, // COMPRESSED_RG_RGTC2
            _ => default
        };
        return true;
    }
}
