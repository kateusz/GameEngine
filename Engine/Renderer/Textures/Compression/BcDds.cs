using BCnEncoder.Shared;
using BCnEncoder.Shared.ImageFiles;

namespace Engine.Renderer.Textures.Compression;

internal static class BcDds
{
    public static bool TryRead(string path, out BcImage image)
    {
        image = default!;
        if (!File.Exists(path))
            return false;

        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(path);
        }
        catch
        {
            return false;
        }

        DdsFile dds;
        try
        {
            dds = DdsFile.Load(new MemoryStream(fileBytes));
        }
        catch
        {
            return false;
        }

        if (dds.Faces.Count == 0 || dds.Faces[0].MipMaps.Length == 0)
            return false;

        var gpuWidth = (int)dds.Faces[0].MipMaps[0].Width;
        var gpuHeight = (int)dds.Faces[0].MipMaps[0].Height;
        if (!BcDdsMetadata.TryRead(fileBytes, out var format, out var sourceWidth, out var sourceHeight))
        {
            sourceWidth = gpuWidth;
            sourceHeight = gpuHeight;
            format = BcGpuFormat.Dxt5Linear;
        }

        var mips = new BcMip[dds.Faces[0].MipMaps.Length];
        for (var i = 0; i < mips.Length; i++)
        {
            var mip = dds.Faces[0].MipMaps[i];
            var w = (int)mip.Width;
            var h = (int)mip.Height;
            if (!ValidateMipSize(format, w, h, mip.Data.Length))
                return false;

            mips[i] = new BcMip(mip.Data, w, h);
        }

        image = new BcImage(format, sourceWidth, sourceHeight, gpuWidth, gpuHeight, mips);
        return true;
    }

    public static bool Write(string path, BcImage image, DdsFile ddsFile)
    {
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory))
                return false;

            Directory.CreateDirectory(directory);
            using var ms = new MemoryStream();
            ddsFile.Write(ms);
            var bytes = ms.ToArray();
            BcDdsMetadata.Patch(bytes, image.Format, image.SourceWidth, image.SourceHeight);

            var tempPath = path + ".tmp";
            File.WriteAllBytes(tempPath, bytes);
            File.Move(tempPath, path, overwrite: true);
            return true;
        }
        catch
        {
            var tempPath = path + ".tmp";
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); }
                catch { }
            }

            return false;
        }
    }

    private static bool ValidateMipSize(BcGpuFormat format, int width, int height, int dataLength)
    {
        var blockBytes = format is BcGpuFormat.Dxt1Srgb ? 8 : 16;
        return dataLength == (width + 3) / 4 * ((height + 3) / 4) * blockBytes;
    }

    internal static CompressionFormat ToBcEncoderFormat(BcGpuFormat format) =>
        format switch
        {
            BcGpuFormat.Dxt1Srgb => CompressionFormat.Bc1,
            BcGpuFormat.Dxt5Srgb or BcGpuFormat.Dxt5Linear => CompressionFormat.Bc3,
            BcGpuFormat.Bc4 => CompressionFormat.Bc4,
            BcGpuFormat.Bc5 => CompressionFormat.Bc5,
            _ => CompressionFormat.Bc3
        };
}
