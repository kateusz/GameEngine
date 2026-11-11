namespace Engine.Renderer.Textures.Compression;

internal static class TextureSidecar
{
    public static string PathFor(string sourcePath) =>
        Path.Combine(
            Path.GetDirectoryName(sourcePath)!,
            Path.GetFileNameWithoutExtension(sourcePath) + ".dds");

    public static bool IsFresh(string sourcePath, string sidecarPath) =>
        File.Exists(sidecarPath)
        && File.GetLastWriteTimeUtc(sidecarPath) >= File.GetLastWriteTimeUtc(sourcePath);
}
