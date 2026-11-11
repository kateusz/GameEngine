using System.Buffers.Binary;

namespace Engine.Renderer.Textures.Compression;

internal static class BcDdsMetadata
{
    private const uint Magic = 0x474E454Du; // 'MENG'

    private const int HeaderSizeOffset = 4;
    private const int Reserved1Offset = HeaderSizeOffset + 28;

    public static void Patch(byte[] ddsBytes, BcGpuFormat format, int sourceWidth, int sourceHeight)
    {
        if (ddsBytes.Length < Reserved1Offset + 16)
            return;

        BinaryPrimitives.WriteUInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset), Magic);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 4), (int)format);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 8), sourceWidth);
        BinaryPrimitives.WriteInt32LittleEndian(ddsBytes.AsSpan(Reserved1Offset + 12), sourceHeight);
    }

    public static bool TryRead(ReadOnlySpan<byte> ddsBytes, out BcGpuFormat format, out int sourceWidth,
        out int sourceHeight)
    {
        format = default;
        sourceWidth = 0;
        sourceHeight = 0;
        if (ddsBytes.Length < Reserved1Offset + 16)
            return false;

        if (BinaryPrimitives.ReadUInt32LittleEndian(ddsBytes.Slice(Reserved1Offset)) != Magic)
            return false;

        var fmt = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 4));
        if (!Enum.IsDefined(typeof(BcGpuFormat), fmt))
            return false;

        format = (BcGpuFormat)fmt;
        sourceWidth = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 8));
        sourceHeight = BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(Reserved1Offset + 12));
        return sourceWidth > 0 && sourceHeight > 0;
    }

    public static int GpuWidth(ReadOnlySpan<byte> ddsBytes) =>
        ddsBytes.Length >= HeaderSizeOffset + 16
            ? BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(HeaderSizeOffset + 12))
            : 0;

    public static int GpuHeight(ReadOnlySpan<byte> ddsBytes) =>
        ddsBytes.Length >= HeaderSizeOffset + 12
            ? BinaryPrimitives.ReadInt32LittleEndian(ddsBytes.Slice(HeaderSizeOffset + 8))
            : 0;
}
