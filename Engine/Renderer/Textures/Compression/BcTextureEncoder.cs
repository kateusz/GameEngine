using BCnEncoder.Encoder;
using BCnEncoder.Shared.ImageFiles;
using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures.Compression;

internal static class BcTextureEncoder
{
    public static (BcImage? Image, DdsFile? Dds) EncodeWithDds(byte[] rgba, int width, int height, BcGpuFormat format,
        bool sRgb, bool isNormal)
    {
        var (padded, gpuWidth, gpuHeight) = BcRgbaPad.Pad(rgba, width, height);
        var encoder = new BcEncoder(BcDds.ToBcEncoderFormat(format));
        encoder.OutputOptions.GenerateMipMaps = true;
        encoder.OutputOptions.Quality = CompressionQuality.Balanced;

        try
        {
            var ddsFile = encoder.EncodeToDds(padded, gpuWidth, gpuHeight, PixelFormat.Rgba32);
            if (ddsFile.Faces.Count == 0 || ddsFile.Faces[0].MipMaps.Length == 0)
                return (null, null);

            var mips = new BcMip[ddsFile.Faces[0].MipMaps.Length];
            for (var i = 0; i < mips.Length; i++)
            {
                var mip = ddsFile.Faces[0].MipMaps[i];
                mips[i] = new BcMip(mip.Data, (int)mip.Width, (int)mip.Height);
            }

            return (new BcImage(format, width, height, gpuWidth, gpuHeight, sRgb, isNormal, mips), ddsFile);
        }
        catch
        {
            return (null, null);
        }
    }

    public static (byte[] Rgba, int Width, int Height) DecodeToRgba(string path, bool sRgb) =>
        TextureRasterToRgba8.Decode(path, sRgb);
}
