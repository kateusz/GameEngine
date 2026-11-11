using BCnEncoder.Encoder;
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

        if (fileBytes.Length < 128 || fileBytes[0] != 'D' || fileBytes[1] != 'D' || fileBytes[2] != 'S' ||
            fileBytes[3] != ' ')
            return false;

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

        if (!BcDdsMetadata.TryRead(fileBytes, out var format, out var sourceWidth, out var sourceHeight))
        {
            sourceWidth = BcDdsMetadata.GpuWidth(fileBytes);
            sourceHeight = BcDdsMetadata.GpuHeight(fileBytes);
            if (sourceWidth <= 0 || sourceHeight <= 0)
                return false;
            format = BcGpuFormat.Dxt5Linear;
        }

        var gpuWidth = BcDdsMetadata.GpuWidth(fileBytes);
        var gpuHeight = BcDdsMetadata.GpuHeight(fileBytes);
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
            var tempPath = path + ".tmp";
            using (var stream = File.Create(tempPath))
                ddsFile.Write(stream);

            var bytes = File.ReadAllBytes(tempPath);
            BcDdsMetadata.Patch(bytes, image.Format, image.SourceWidth, image.SourceHeight);
            File.WriteAllBytes(tempPath, bytes);
            File.Move(tempPath, path, overwrite: true);
            return true;
        }
        catch
        {
            var tempPath = path + ".tmp";
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                }
            }

            return false;
        }
    }

    private static bool ValidateMipSize(BcGpuFormat format, int width, int height, int dataLength)
    {
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var blockBytes = format is BcGpuFormat.Dxt1Srgb ? 8 : 16;
        return dataLength == blocksWide * blocksHigh * blockBytes;
    }

    internal static CompressionFormat ToBcEncoderFormat(BcGpuFormat format) =>
        format switch
        {
            BcGpuFormat.Dxt1Srgb => CompressionFormat.Bc1,
            BcGpuFormat.Dxt5Srgb => CompressionFormat.Bc3,
            BcGpuFormat.Dxt5Linear => CompressionFormat.Bc3,
            BcGpuFormat.Bc4 => CompressionFormat.Bc4,
            BcGpuFormat.Bc5 => CompressionFormat.Bc5,
            _ => CompressionFormat.Bc3
        };
}
