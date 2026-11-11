using BCnEncoder.Encoder;
using BCnEncoder.Encoder.Options;
using BCnEncoder.Shared.ImageFiles;
using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures.Compression;

internal static class BcTextureEncoder
{
    public static (BcImage? Image, DdsFile? Dds) EncodeWithDds(byte[] rgba, int width, int height, BcGpuFormat format)
    {
        var (padded, gpuWidth, gpuHeight) = BcRgbaPad.Pad(rgba, width, height);
        var compression = BcDds.ToBcEncoderFormat(format);
        var encoder = new BcEncoder(compression);
        encoder.OutputOptions.GenerateMipMaps = true;
        encoder.OutputOptions.Quality = CompressionQuality.Balanced;

        try
        {
            var ddsFile = encoder.EncodeToDds(padded, gpuWidth, gpuHeight, BCnEncoder.Encoder.PixelFormat.Rgba32);
            if (ddsFile.Faces.Count == 0 || ddsFile.Faces[0].MipMaps.Length == 0)
                return (null, null);

            var mips = new BcMip[ddsFile.Faces[0].MipMaps.Length];
            for (var i = 0; i < mips.Length; i++)
            {
                var mip = ddsFile.Faces[0].MipMaps[i];
                mips[i] = new BcMip(mip.Data, (int)mip.Width, (int)mip.Height);
            }

            var image = new BcImage(format, width, height, gpuWidth, gpuHeight, mips);
            return (image, ddsFile);
        }
        catch
        {
            return (null, null);
        }
    }

    public static byte[] DecodeToRgba(string path, bool sRgb)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        var bytesPerPixel = decoded.Data.Length / (decoded.Width * decoded.Height);
        return (decoded.DataFormat, bytesPerPixel) switch
        {
            (Silk.NET.OpenGL.PixelFormat.Bgra, 4) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 4),
            (Silk.NET.OpenGL.PixelFormat.Bgr, 3) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 3),
            _ => decoded.Data
        };
    }
}
