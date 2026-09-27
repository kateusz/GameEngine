using System.Security.Cryptography;

namespace Engine.Renderer.Models.RuntimeMesh;

internal readonly record struct RuntimeMeshStamp(
    ulong SourceSize,
    byte[] SourceSha256,
    uint ImporterVersion,
    uint PostProcessFlags)
{
    public bool Matches(RuntimeMeshStamp other) =>
        SourceSize == other.SourceSize
        && ImporterVersion == other.ImporterVersion
        && PostProcessFlags == other.PostProcessFlags
        && SourceSha256.AsSpan().SequenceEqual(other.SourceSha256);

    public static RuntimeMeshStamp FromFile(string sourcePath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        return FromBytes(bytes);
    }

    private static RuntimeMeshStamp FromBytes(ReadOnlySpan<byte> sourceBytes) =>
        new(
            (ulong)sourceBytes.Length,
            SHA256.HashData(sourceBytes),
            RuntimeMeshFormat.ImporterVersion,
            RuntimeMeshFormat.AssimpPostProcessFlags);
}
