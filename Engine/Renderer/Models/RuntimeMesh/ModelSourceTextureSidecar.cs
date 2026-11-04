using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace Engine.Renderer.Models.RuntimeMesh;

/// <summary>
/// During source import: writes embedded Assimp textures into the model's <c>.tex</c> sidecar folder
/// and returns relative paths suitable for <see cref="SourceSubmesh"/> / GEM1 material strings.
/// </summary>
internal sealed class ModelSourceTextureSidecar(string sourceFilePath)
{
    private static readonly ILogger Logger = Log.ForContext<ModelSourceTextureSidecar>();

    private readonly string _sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(sourceFilePath)) ?? string.Empty;
    private readonly string _textureFolder = RuntimeMeshPaths.TextureFolderPath(Path.GetFullPath(sourceFilePath));
    private readonly Dictionary<uint, string> _embeddedByIndex = new();

    public string ToRelativeTexturePath(string? resolvedAbsolutePath)
    {
        if (string.IsNullOrEmpty(resolvedAbsolutePath))
            return string.Empty;

        var relative = RuntimeMeshPaths.ToRelativeAssetPath(_sourceDirectory, resolvedAbsolutePath);
        if (relative == null)
        {
            Logger.Warning("Texture path outside source directory: {Path}", resolvedAbsolutePath);
            return string.Empty;
        }

        return relative;
    }

    public unsafe string PersistEmbeddedTexture(Silk.NET.Assimp.Scene* scene, string embeddedRef)
    {
        if (!TryParseEmbeddedIndex(embeddedRef, out var index) || scene->MNumTextures <= index)
            return string.Empty;

        // The same *index is referenced by many materials. Re-copying it later in the
        // import has come back one bit off, and that PNG is what stb rejects.
        if (_embeddedByIndex.TryGetValue(index, out var cached))
            return cached;

        var absolute = ExtractEmbeddedTexture(scene, embeddedRef, _textureFolder);
        if (absolute == null)
            return string.Empty;

        var relative = ToRelativeTexturePath(absolute);
        if (relative.Length > 0)
            _embeddedByIndex[index] = relative;
        return relative;
    }

    internal static unsafe string? ExtractEmbeddedTexture(
        Silk.NET.Assimp.Scene* scene,
        string embeddedRef,
        string targetDirectory)
    {
        if (!TryParseEmbeddedIndex(embeddedRef, out var index) || index >= scene->MNumTextures)
            return null;

        var tex = scene->MTextures[index];
        if (tex == null)
            return null;

        if (tex->MHeight != 0)
        {
            Logger.Warning(
                "Embedded texture {Ref} is uncompressed ({W}x{H}) — not supported yet",
                embeddedRef, tex->MWidth, tex->MHeight);
            return null;
        }

        var byteCount = (int)tex->MWidth;
        if (byteCount <= 0 || tex->PcData == null)
            return null;

        byte[]? bytes = null;
        for (var attempt = 0; attempt < 3 && bytes == null; attempt++)
        {
            var copy = new byte[byteCount];
            fixed (byte* dst = copy)
                Buffer.MemoryCopy(tex->PcData, dst, byteCount, byteCount);
            if (IsIntactImage(copy))
                bytes = copy;
        }

        if (bytes == null)
        {
            Logger.Warning("Embedded texture {Ref} failed image check", embeddedRef);
            return null;
        }

        var ext = GuessImageExtension(bytes, ReadFormatHint(tex));
        Directory.CreateDirectory(targetDirectory);
        var hash = Convert.ToHexString(SHA256.HashData(bytes))[..16];
        var cachePath = Path.Combine(targetDirectory, hash + ext);
        if (!File.Exists(cachePath))
            File.WriteAllBytes(cachePath, bytes);

        return cachePath;
    }

    internal static bool IsIntactImage(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 8 || bytes[0] != 0x89 || bytes[1] != (byte)'P' || bytes[2] != (byte)'N' || bytes[3] != (byte)'G')
            return true;

        var pos = 8;
        var sawEnd = false;
        while (pos + 12 <= bytes.Length)
        {
            var length = ReadBigEndian(bytes, pos);
            if (pos + 12L + length > bytes.Length)
                return false;

            var crcAt = pos + 8 + (int)length;
            var expected = ReadBigEndian(bytes, crcAt);
            if (expected != Crc32(bytes.Slice(pos + 4, 4 + (int)length)))
                return false;

            if (bytes[pos + 4] == (byte)'I' && bytes[pos + 5] == (byte)'E' &&
                bytes[pos + 6] == (byte)'N' && bytes[pos + 7] == (byte)'D')
            {
                sawEnd = true;
                break;
            }

            pos = crcAt + 4;
        }

        return sawEnd;
    }

    private static uint ReadBigEndian(ReadOnlySpan<byte> bytes, int pos) =>
        ((uint)bytes[pos] << 24) | ((uint)bytes[pos + 1] << 16) | ((uint)bytes[pos + 2] << 8) | bytes[pos + 3];

    private static readonly uint[] CrcTable = CreateCrcTable();

    private static uint Crc32(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        return ~crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            var c = i;
            for (var bit = 0; bit < 8; bit++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            table[i] = c;
        }

        return table;
    }

    private static bool TryParseEmbeddedIndex(string embeddedRef, out uint index)
    {
        index = 0;
        if (embeddedRef.Length < 2 || embeddedRef[0] != '*')
            return false;

        var indexSpan = embeddedRef.AsSpan(1);
        var colon = indexSpan.IndexOf(':');
        if (colon >= 0)
            indexSpan = indexSpan[..colon];
        return uint.TryParse(indexSpan, out index);
    }

    private static string GuessImageExtension(byte[] bytes, string hint)
    {
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G')
            return ".png";
        if (bytes is [0xFF, 0xD8, ..])
            return ".jpg";
        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";

        if (hint.Length is > 0 and <= 4 && hint.All(char.IsLetterOrDigit))
            return "." + hint.ToLowerInvariant();

        return ".bin";
    }

    private static unsafe string ReadFormatHint(Silk.NET.Assimp.Texture* tex)
    {
        var sb = new StringBuilder(8);
        var p = (byte*)&tex->AchFormatHint;
        for (var i = 0; i < 8; i++)
        {
            var c = p[i];
            if (c == 0)
                break;
            if (c is < 32 or > 126)
                return string.Empty;
            sb.Append((char)c);
        }

        return sb.ToString();
    }
}
