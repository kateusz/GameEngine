namespace Engine.Renderer.Textures.Compression;

internal static class BcDdsSidecarPath
{
    public static string ForSource(string sourcePath, bool sRgb) =>
        sourcePath + (sRgb ? ".srgb.dds" : ".linear.dds");
}
