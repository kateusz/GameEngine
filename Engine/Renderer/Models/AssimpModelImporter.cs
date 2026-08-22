using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Engine.Renderer.Textures;
using Serilog;
using Silk.NET.Assimp;
using AssimpMesh = Silk.NET.Assimp.Mesh;
using Mesh = Engine.Renderer.Meshes.Mesh;

namespace Engine.Renderer.Models;

internal sealed class AssimpModelImporter(ITextureFactory textureFactory)
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();
    
    private static readonly string EmbeddedCacheDir =
        Path.Combine(Path.GetTempPath(), "GameEngine", "embedded-textures");
    
    private readonly Assimp _assimp = Assimp.GetApi();

    public List<Mesh> Import(string path)
    {
        var submeshes = new List<Mesh>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.FlipUVs |
                                  PostProcessSteps.PreTransformVertices);

        unsafe
        {
            var scene = _assimp.ImportFile(path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, _assimp.GetErrorStringS());
                return submeshes;
            }

            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                var aiMesh = scene->MMeshes[i];
                var mesh = ExtractMesh(aiMesh);
                ExtractMaterial(mesh, scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(mesh);
            }

            _assimp.ReleaseImport(scene);
        }

        return submeshes;
    }

    private unsafe Mesh ExtractMesh(AssimpMesh* aiMesh)
    {
        var mesh = new Mesh(aiMesh->MName.AsString);

        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex
            {
                Position = aiMesh->MVertices[i]
            };

            if (aiMesh->MNormals != null)
                vertex.Normal = aiMesh->MNormals[i];

            if (hasTangents)
                vertex.Tangent = aiMesh->MTangents[i];

            if (aiMesh->MBitangents != null)
                vertex.Bitangent = aiMesh->MBitangents[i];

            if (hasTexCoords)
            {
                var texcoord3 = aiMesh->MTextureCoords[0][i];
                vertex.TexCoord = new Vector2(texcoord3.X, texcoord3.Y);
            }

            mesh.Vertices.Add(vertex);
        }

        for (uint i = 0; i < aiMesh->MNumFaces; i++)
        {
            var face = aiMesh->MFaces[i];
            for (uint j = 0; j < face.MNumIndices; j++)
                mesh.Indices.Add(face.MIndices[j]);
        }

        return mesh;
    }

    private unsafe void ExtractMaterial(Mesh mesh, Silk.NET.Assimp.Scene* scene, uint materialIndex,
        string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];

        var diffuseTexturePath = ResolveTexturePath(scene, aiMaterial, TextureType.Diffuse, directory);
        var specularTexturePath = ResolveTexturePath(scene, aiMaterial, TextureType.Specular, directory);
        var normalTexturePath = ResolveTexturePath(scene, aiMaterial, TextureType.Normals, directory)
                                ?? ResolveTexturePath(scene, aiMaterial, TextureType.Height, directory);
        mesh.DiffuseTexture = LoadTexture(diffuseTexturePath);
        mesh.SpecularTexture = LoadTexture(specularTexturePath);
        mesh.NormalTexture = LoadTexture(normalTexturePath);

        var shininess = 32.0f;
        _assimp.GetMaterialFloatArray(aiMaterial, Assimp.MaterialShininess, 0, 0, ref shininess, (uint*)null);
        mesh.Shininess = shininess > 0 ? shininess : 32.0f;
    }

    private unsafe string? ResolveTexturePath(Silk.NET.Assimp.Scene* scene, Material* aiMaterial,
        TextureType textureType,
        string directory)
    {
        if (_assimp.GetMaterialTextureCount(aiMaterial, textureType) == 0)
            return null;

        AssimpString aiPath;
        var result = _assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
        {
            Logger.Warning("GetMaterialTexture failed type={Type} result={Result}", textureType, result);
            return null;
        }
        
        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
        {
            Logger.Warning("Texture type={Type} has empty path", textureType);
            return null;
        }

        // GLB/glTF embedded images show up as "*0", "*1", …
        if (texturePath.StartsWith('*'))
        {
            var cached = ExtractEmbeddedTextureToCache(scene, texturePath);
            if (cached == null)
            {
                Logger.Warning("Failed to extract embedded texture type={Type} ref={Ref}", textureType, texturePath);
                return null;
            }

            Logger.Debug("Texture type={Type} extracted embedded {Ref} → {Path}", textureType, texturePath, cached);
            return cached;
        }

        var resolved = AssimpTexturePath.Resolve(texturePath, directory);
        if (resolved == null)
        {
            Logger.Warning(
                "Texture type={Type} path missing on disk: {Path}",
                textureType, texturePath);
            return null;
        }

        Logger.Debug("Texture type={Type} resolved to file {Path}", textureType, resolved);
        return resolved;
    }
    
    private static unsafe string? ExtractEmbeddedTextureToCache(Silk.NET.Assimp.Scene* scene, string embeddedRef)
    {
        // Native Assimp shipped with Silk.NET may lack aiGetEmbeddedTexture — index into MTextures instead.
        // Refs look like "*0" or "*0:filename.png".
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

        // Compressed image blob (png/jpg/…)
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
            System.Buffer.MemoryCopy(tex->PcData, dst, byteCount, byteCount);

        var ext = GuessImageExtension(bytes, ReadFormatHint(tex));
        Directory.CreateDirectory(EmbeddedCacheDir);
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).AsSpan(0, 16);
        var cachePath = Path.Combine(EmbeddedCacheDir, $"{hash}{ext}");
        if (!System.IO.File.Exists(cachePath))
            System.IO.File.WriteAllBytes(cachePath, bytes);

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

        // Assimp AchFormatHint is often garbage on GLB — only trust short alphanumeric hints.
        if (hint.Length is > 0 and <= 4 && hint.All(char.IsLetterOrDigit))
            return "." + hint.ToLowerInvariant();

        return ".bin";
    }

    private static unsafe string ReadFormatHint(Silk.NET.Assimp.Texture* tex)
    {
        // Assimp stores up to 8 chars + null in AchFormatHint.
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
    
    private Texture2D? LoadTexture(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return textureFactory.Create(path);
        }
        catch (Exception ex)
        {
            Logger.Warning(ex, "Failed to load texture {Path}", path);
            return null;
        }
    }
}
