using Engine.Renderer.Textures.Compression;
using InternalFormat = Silk.NET.OpenGL.InternalFormat;

namespace Engine.Platform.OpenGL;

internal static class BcGlInternalFormat
{
    public static bool TryGet(BcGpuFormat format, out InternalFormat internalFormat)
    {
        if (format is BcGpuFormat.Dxt1Srgb or BcGpuFormat.Dxt5Srgb or BcGpuFormat.Dxt5Linear)
        {
            if (!OpenGLTextureCaps.SupportsS3Tc)
            {
                internalFormat = default;
                return false;
            }
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
