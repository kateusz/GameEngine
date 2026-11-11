using BCnEncoder.Encoder;
using BCnEncoder.Shared.ImageFiles;
using Engine.Platform.OpenGL;

namespace Engine.Renderer.Textures.Compression;

internal static class BcTextureEncoder
{
    public static (BcImage? Image, DdsFile? Dds) EncodeWithDds(byte[] rgba, int width, int height, BcGpuFormat format)
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

            return (new BcImage(format, width, height, gpuWidth, gpuHeight, mips), ddsFile);
        }
        catch
        {
            return (null, null);
        }
    }

    public static (byte[] Rgba, int Width, int Height) DecodeToRgba(string path, bool sRgb)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        var bytesPerPixel = decoded.Data.Length / (decoded.Width * decoded.Height);
        var rgba = (decoded.DataFormat, bytesPerPixel) switch
        {
            (Silk.NET.OpenGL.PixelFormat.Bgra, 4) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 4),
            (Silk.NET.OpenGL.PixelFormat.Bgr, 3) => TexturePreviewScaling.ToPackedRgba(decoded.Data, 3),
            _ => decoded.Data
        };
        return (rgba, decoded.Width, decoded.Height);
    }
}
