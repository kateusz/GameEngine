using System.Buffers.Binary;

namespace Engine.Renderer.Textures.Compression;

internal static class BcDdsMetadata
{
    private const uint Magic = 0x474E454Du; // 'MENG'
    private const int Reserved1Offset = 32;
    private const int ImportSRgbOffset = Reserved1Offset + 16;
    private const int ImportIsNormalOffset = Reserved1Offset + 20;
    internal const int HeaderLength = ImportIsNormalOffset + 4;

    public static void Patch(byte[] ddsBytes, BcImage image)
    {
        if (ddsBytes.Length < HeaderLength)
            return;

        BinaryPrimitives.WriteUInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset), Magic);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 4), (int)image.Format);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 8), image.SourceWidth);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 12), image.SourceHeight);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(ImportSRgbOffset), image.SRgb ? 1 : 0);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(ImportIsNormalOffset), image.IsNormal ? 1 : 0);
    }

    public static bool TryRead(ReadOnlySpan<byte> ddsBytes, out BcImage image)
    {
        image = default!;
        if (ddsBytes.Length < HeaderLength)
            return false;
        if (BinaryPrimitives.ReadUInt32LittleEndian(ddsBytes.Slice(Reserved1Offset)) != Magic)
            return false;

        var fmt = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 4));
        if (!Enum.IsDefined(typeof(BcGpuFormat), fmt))
            return false;

        var sourceWidth = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 8));
        var sourceHeight = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 12));
        if (sourceWidth <= 0 || sourceHeight <= 0)
            return false;

        var sRgb = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(ImportSRgbOffset)) != 0;
        var isNormal = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(ImportIsNormalOffset)) != 0;
        image = new BcImage((BcGpuFormat)fmt, sourceWidth, sourceHeight, 0, 0, sRgb, isNormal, []);
        return true;
    }
}
