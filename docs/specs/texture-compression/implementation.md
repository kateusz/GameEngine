# Texture compression (BC / DXT) — Implementation sketch

C# and GLSL sketch for this engine. Do not treat this file as the source tree.

## Where it sits

- **No component. No system priority.** Load stays `ITextureFactory.Create`.
- **DryIoc:** existing `TextureFactory` registration. Inject `EngineHostOptions` so the factory may write DDS siblings on load.
- **Serialization:** none. Sidecar is a file next to the source.
- **Editor:** ensure sibling on `Create` when `EnsureTextureDdsSiblingOnLoad` is true (`EngineHostOptions.EditorDefaults`).
- **Runtime player:** flag false; compressed upload only.
- **Publish:** `GamePublisher` calls `TextureBcSidecar.TryEnsureUpToDate` on assets before `CopyAssets`.
- **OpenGL:** only `OpenGLTexture2D` / a small compressed-upload helper in `Platform/OpenGL`.

## Host flag

```csharp
public sealed record EngineHostOptions(...)
{
    /// <summary>
    /// Editor and publish: encode a sibling .dds when missing or older than the source.
    /// Runtime player: false.
    /// </summary>
    public bool EnsureTextureDdsSiblingOnLoad { get; init; }
}
```

Set `true` on `EditorDefaults`. Leave default `false` for the player.

## CPU types (Engine, no Silk GL)

```csharp
internal enum BcGpuFormat
{
    Dxt1Srgb,
    Dxt5Srgb,
    Dxt5Linear,
    Bc4,
    Bc5
}

internal readonly record struct BcMip(byte[] Blocks, int Width, int Height);

internal readonly record struct BcImage(
    BcGpuFormat Format,
    int SourceWidth,
    int SourceHeight,
    int GpuWidth,
    int GpuHeight,
    BcMip[] Mips);

internal static class BcFormatPick
{
    public static BcGpuFormat Pick(bool sRgb, bool isNormalMap, ReadOnlySpan<byte> rgba, int width, int height)
    {
        if (sRgb)
            return HasAlpha(rgba) ? BcGpuFormat.Dxt5Srgb : BcGpuFormat.Dxt1Srgb;
        if (isNormalMap)
            return BcGpuFormat.Bc5;
        if (IsSingleChannel(rgba))
            return BcGpuFormat.Bc4;
        return BcGpuFormat.Dxt5Linear;
    }
}

internal static class TextureSidecar
{
    public static string PathFor(string sourcePath) =>
        System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(sourcePath)!,
            System.IO.Path.GetFileNameWithoutExtension(sourcePath) + ".dds");

    public static bool IsFresh(string sourcePath, string sidecarPath) =>
        File.Exists(sidecarPath)
        && File.GetLastWriteTimeUtc(sidecarPath) >= File.GetLastWriteTimeUtc(sourcePath);
}
```

Automatic sidecars use block-aligned rasters. Other rasters keep their original dimensions for repeat sampling. The encoder builds mipmaps and returns a `BcImage`. `BcDds.Write` writes a temporary file and replaces an existing sibling only if it carries engine metadata. `BcDds.TryRead` validates the byte count of each mip against its rounded-up block dimensions.

Block bytes: DXT1 and BC4 = 8; DXT5 and BC5 = 16.

`isNormalMap` is the same boolean the model loader already has when it calls `Create(path, sRgb: false)` for the normal path. Sprites always pass `sRgb: true`. Publish’s folder walk infers normal vs data from the suffixes `normal`, `nrm`, `orm`, `metallic`, `roughness`, `occlusion`, `ao` (case-insensitive, in the file name).

## Factory

```csharp
public Texture2D Create(string path, bool sRgb = false)
{
    var cacheKey = ToCacheKey(path, sRgb);
    lock (_cacheLock)
    {
        if (_textureCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var texture = LoadWithDdsSibling(path, sRgb);
        _textureCache[cacheKey] = texture;
        return texture;
    }
}

private Texture2D LoadWithDdsSibling(string path, bool sRgb)
{
    var sidecar = TextureSidecar.PathFor(path);
    if (TextureSidecar.IsFresh(path, sidecar) && BcDds.TryRead(sidecar, out var bc))
        return OpenGLTexture2D.CreateCompressed(path, bc);

    if (_hostOptions.EnsureTextureDdsSiblingOnLoad)
    {
        var decoded = TextureFileDecoder.Decode(path, sRgb);
        var rgba = ToRgba(decoded);
        var format = BcFormatPick.Pick(sRgb, IsNormalLoad(path, sRgb), rgba, decoded.Width, decoded.Height);
        var image = BcEncoder.Encode(rgba, decoded.Width, decoded.Height, format);
        BcDds.Write(sidecar, image);
        return OpenGLTexture2D.CreateCompressed(path, image);
    }

    Logger.Warning("BC sibling missing or stale for {Path}; uploading uncompressed", path);
    return OpenGLTexture2D.Create(path, sRgb);
}
```

`Decode` / `DecodePreview` / `CreateFromRgba` unchanged. A source path that already ends in `.dds` and parses as BC skips cook and calls `CreateCompressed` directly (stop Pfim decompress on that GPU path).

## OpenGL upload

```csharp
public static Texture2D CreateCompressed(string path, BcImage image)
{
    var handle = SilkNetContext.GL.GenTexture();
    SilkNetContext.GL.BindTexture(TextureTarget.Texture2D, handle);
    var internalFormat = ToInternalFormat(image.Format); // maps enum → InternalFormat
    for (var i = 0; i < image.Mips.Length; i++)
    {
        var mip = image.Mips[i];
        unsafe
        {
            fixed (byte* ptr = mip.Blocks)
            {
                SilkNetContext.GL.CompressedTexImage2D(
                    TextureTarget.Texture2D,
                    i,
                    internalFormat,
                    (uint)mip.Width,
                    (uint)mip.Height,
                    0,
                    (uint)mip.Blocks.Length,
                    ptr);
            }
        }
    }
    var min = image.Mips.Length > 1
        ? TextureMinFilter.LinearMipmapLinear
        : TextureMinFilter.Linear;
    SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)min);
    SilkNetContext.GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
    // wrap REPEAT + anisotropy: same as UploadTexture today
    return new OpenGLTexture2D(path, handle, image.SourceWidth, image.SourceHeight, internalFormat, PixelFormat.Rgba);
}
```

Query S3TC once (`GL.IsExtensionPresent("GL_EXT_texture_compression_s3tc")` or the EXT string the driver actually exposes). If false, `CreateCompressed` for DXT formats must not run; factory uses RGBA.

Why: LearnOpenGL texture upload is gen/bind/`TexImage2D`/filters. Compressed textures replace `TexImage2D` with `CompressedTexImage2D` and pass byte size instead of `PixelFormat` + `UnsignedByte`. No `GenerateMipmap`.

## Normal reconstruct

In `modelShader.frag`, when `u_HasNormalMap != 0`:

```glsl
vec3 nts = texture(u_NormalMap, v_TexCoord).rgb * 2.0 - 1.0;
nts.z = sqrt(max(1.0 - nts.x * nts.x - nts.y * nts.y, 0.0));
vec3 norm = normalize(v_TBN * nts);
```

Why: BC5 stores only XY. A DXT5 normal still has Z in blue; reconstructing from XY is valid for both and matches a unit tangent-space normal.

## Publish

```csharp
private PublishResult EnsureTextureDdsSiblings()
{
    var assets = Path.Combine(projectContext.Root!, "assets");
    foreach (var file in Directory.EnumerateFiles(assets, "*.*", SearchOption.AllDirectories))
    {
        if (!TextureBcSidecar.TryEnsureUpToDate(file, out var error))
            return PublishResult.Failed(error);
    }
    return PublishResult.Ok();
}
```

Call immediately before `CopyAssets`. `TextureBcSource` picks `sRgb` / normal from the file name; encode+write matches the factory.

## Tests worth adding

- `BcFormatPick`: opaque sRGB → DXT1; alpha → DXT5 sRGB; normal → BC5; one-channel linear → BC4; MR-like RGBA linear → DXT5 linear.
- `TextureSidecar.IsFresh`: source newer → false; sidecar newer → true.
- `BcDds` round trip: 8×8 DXT5, mip count, `SourceWidth` preserved when GPU is padded from 6×6.
- Factory with cook flag false and no sibling: uncompressed path, file not created (temp dir).
- `GamePublisher` fixture: after cook, each png has `{stem}.dds`; encoder stub failure → `Success == false`.
- Graphics fact (skip on macOS CI if no GL): 4×4 DXT5 `CompressedTexImage2D` succeeds when S3TC is present.

## Cheap win (keep)

Skip Pfim decompress on BC GPU upload. Skip `GenerateMipmap` when the sibling has mips. That is the 4K hitch, not a new pipeline.
