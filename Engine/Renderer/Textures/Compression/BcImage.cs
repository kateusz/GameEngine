namespace Engine.Renderer.Textures.Compression;

internal readonly record struct BcMip(byte[] Blocks, int Width, int Height);

internal readonly record struct BcImage(
    BcGpuFormat Format,
    int SourceWidth,
    int SourceHeight,
    int GpuWidth,
    int GpuHeight,
    BcMip[] Mips);
