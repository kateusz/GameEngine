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
        var absolute = ExtractEmbeddedTexture(scene, embeddedRef, _textureFolder);
        if (absolute == null)
            return string.Empty;

        return ToRelativeTexturePath(absolute);
    }

    internal static unsafe string? ExtractEmbeddedTexture(
        Silk.NET.Assimp.Scene* scene,
        string embeddedRef,
        string targetDirectory)
    {
        if (embeddedRef.Length < 2 || embeddedRef[0] != '*')
            return null;

        var indexSpan = embeddedRef.AsSpan(1);
        var colon = indexSpan.IndexOf(':');
        if (colon >= 0)
            indexSpan = indexSpan[..colon];

        if (!uint.TryParse(indexSpan, out var index) || index >= scene->MNumTextures)
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

        var bytes = new byte[byteCount];
        fixed (byte* dst = bytes)
            Buffer.MemoryCopy(tex->PcData, dst, byteCount, byteCount);

        var ext = GuessImageExtension(bytes, ReadFormatHint(tex));
        Directory.CreateDirectory(targetDirectory);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).AsSpan(0, 16);
        var cachePath = Path.Combine(targetDirectory, $"{hash}{ext}");
        if (!File.Exists(cachePath))
            File.WriteAllBytes(cachePath, bytes);

        return cachePath;
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
