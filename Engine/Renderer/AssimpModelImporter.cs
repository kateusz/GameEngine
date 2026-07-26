using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Serilog;
using Silk.NET.Assimp;

namespace Engine.Renderer;

internal sealed class AssimpModelImporter(Assimp assimp)
{
    private static readonly ILogger Logger = Log.ForContext<AssimpModelImporter>();
    private static readonly string EmbeddedCacheDir =
        Path.Combine(Path.GetTempPath(), "GameEngine", "embedded-textures");

    /// <summary>
    /// Post-process for skinned cook: never <see cref="PostProcessSteps.PreTransformVertices"/>
    /// (destroys hierarchy); always <see cref="PostProcessSteps.LimitBoneWeights"/> (≤4).
    /// </summary>
    public const PostProcessSteps SkinnedPostProcessFlags =
        PostProcessSteps.Triangulate |
        PostProcessSteps.GenerateNormals |
        PostProcessSteps.CalculateTangentSpace |
        PostProcessSteps.JoinIdenticalVertices |
        PostProcessSteps.LimitBoneWeights;

    public static int MaxSkeletonBones => (int)SkeletonReader.MaxBones;

    public List<ModelSubmesh> Import(string path)
    {
        var submeshes = new List<ModelSubmesh>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        // glTF/GLB UVs are already OpenGL-style (v=0 bottom). Do NOT FlipUVs here —
        // textures are uploaded with stbi_flip; FlipUVs + flip = double-flip atlas scramble.
        // PreTransformVertices keeps single-file Create in a common space (no hierarchy).
        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace |
                                  PostProcessSteps.PreTransformVertices);

        unsafe
        {
            var scene = AssimpSceneImport.Import(assimp, path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return submeshes;
            }

            Logger.Information(
                "Assimp import ok path={Path} meshes={MeshCount} materials={MaterialCount} embeddedTextures={EmbeddedCount} flags={Flags}",
                path, scene->MNumMeshes, scene->MNumMaterials, scene->MNumTextures, scene->MFlags);

            for (uint i = 0; i < scene->MNumMeshes; i++)
            {
                var aiMesh = scene->MMeshes[i];
                var mesh = ExtractMesh(aiMesh);
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(new ModelSubmesh(mesh, material));
            }

            assimp.ReleaseImport(scene);
        }

        return submeshes;
    }

    /// <summary>
    /// Node walk without <see cref="PostProcessSteps.PreTransformVertices"/> —
    /// one part per mesh-bearing node with transform relative to the Assimp root.
    /// </summary>
    public List<AssimpModelPart> ImportParts(string path)
    {
        var parts = new List<AssimpModelPart>();
        var directory = Path.GetDirectoryName(path) ?? string.Empty;

        const uint flags = (uint)(PostProcessSteps.Triangulate |
                                  PostProcessSteps.GenerateNormals |
                                  PostProcessSteps.CalculateTangentSpace);

        unsafe
        {
            var scene = AssimpSceneImport.Import(assimp, path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                Logger.Error(
                    "Failed to import model parts path={Path} assimpError={AssimpError}",
                    path, assimp.GetErrorStringS());
                return parts;
            }

            Logger.Information(
                "Assimp import parts ok path={Path} meshes={MeshCount} materials={MaterialCount} embeddedTextures={EmbeddedCount}",
                path, scene->MNumMeshes, scene->MNumMaterials, scene->MNumTextures);

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            WalkNode(scene, scene->MRootNode, Matrix4x4.Identity, directory, nameCounts, parts, boneIndexByName: null);

            assimp.ReleaseImport(scene);
        }

        return parts;
    }

    /// <summary>
    /// Skinned cook: full node walk, bone weights, skeleton + clips.
    /// </summary>
    /// <exception cref="InvalidOperationException">No bones, or bone count &gt; <see cref="MaxSkeletonBones"/>.</exception>
    public AssimpSkinnedImport ImportSkinned(string path)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var flags = (uint)SkinnedPostProcessFlags;

        unsafe
        {
            var scene = AssimpSceneImport.Import(assimp, path, flags);

            if (scene == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0 || scene->MRootNode == null)
            {
                var err = assimp.GetErrorStringS();
                Logger.Error("Failed to import skinned model path={Path} assimpError={AssimpError}", path, err);
                throw new InvalidOperationException($"Assimp failed to import skinned model: {err}");
            }

            Logger.Information(
                "Assimp import skinned ok path={Path} meshes={MeshCount} animations={AnimCount}",
                path, scene->MNumMeshes, scene->MNumAnimations);

            try
            {
                var nodesByName = new Dictionary<string, nint>(StringComparer.Ordinal);
                CollectNodesByName(scene->MRootNode, nodesByName);

                var skeleton = BuildSkeleton(scene, nodesByName);
                var boneIndexByName = new Dictionary<string, int>(skeleton.Bones.Count, StringComparer.Ordinal);
                for (var i = 0; i < skeleton.Bones.Count; i++)
                    boneIndexByName[skeleton.Bones[i].Name] = i;

                var parts = new List<AssimpModelPart>();
                var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                WalkNode(scene, scene->MRootNode, Matrix4x4.Identity, directory, nameCounts, parts, boneIndexByName);

                if (parts.Count == 0)
                    throw new InvalidOperationException($"Assimp produced no mesh nodes for skinned model: {path}");

                var animations = ExtractAnimations(scene, boneIndexByName);
                return new AssimpSkinnedImport(parts, skeleton, animations);
            }
            finally
            {
                assimp.ReleaseImport(scene);
            }
        }
    }

    private unsafe void WalkNode(
        Silk.NET.Assimp.Scene* scene,
        Node* node,
        Matrix4x4 parentWorld,
        string directory,
        Dictionary<string, int> nameCounts,
        List<AssimpModelPart> parts,
        Dictionary<string, int>? boneIndexByName)
    {
        // Assimp: node transform maps local → parent (column-vector storage).
        // Accumulated in Assimp space; transposed to Numerics when emitting parts.
        var world = Matrix4x4.Multiply(parentWorld, node->MTransformation);

        if (node->MNumMeshes > 0 && node->MMeshes != null)
        {
            var submeshes = new List<ModelSubmesh>((int)node->MNumMeshes);
            for (uint i = 0; i < node->MNumMeshes; i++)
            {
                var meshIndex = node->MMeshes[i];
                if (meshIndex >= scene->MNumMeshes)
                    continue;

                var aiMesh = scene->MMeshes[meshIndex];
                var mesh = boneIndexByName != null
                    ? ExtractSkinnedMesh(aiMesh, boneIndexByName)
                    : ExtractMesh(aiMesh);
                var material = ExtractMaterial(scene, aiMesh->MMaterialIndex, directory);
                submeshes.Add(new ModelSubmesh(mesh, material));
            }

            if (submeshes.Count > 0)
            {
                var rawName = node->MName.AsString;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = submeshes[0].Mesh.Name;
                if (string.IsNullOrWhiteSpace(rawName))
                    rawName = "part";

                var partName = AssimpPartNaming.UniqueSanitize(rawName, nameCounts);
                // Assimp aiMatrix4x4 is column-vector (T in 4th column). System.Numerics /
                // TransformComponent use row-vector (T in 4th row) — transpose before TRS.
                parts.Add(new AssimpModelPart(partName, Matrix4x4.Transpose(world), submeshes));
            }
        }

        for (uint i = 0; i < node->MNumChildren; i++)
        {
            var child = node->MChildren[i];
            if (child != null)
                WalkNode(scene, child, world, directory, nameCounts, parts, boneIndexByName);
        }
    }

    private static unsafe void CollectNodesByName(Node* node, Dictionary<string, nint> nodesByName)
    {
        var name = node->MName.AsString;
        if (!string.IsNullOrEmpty(name))
            nodesByName.TryAdd(name, (nint)node);

        for (uint i = 0; i < node->MNumChildren; i++)
        {
            var child = node->MChildren[i];
            if (child != null)
                CollectNodesByName(child, nodesByName);
        }
    }

    private static unsafe SkeletonAsset BuildSkeleton(
        Silk.NET.Assimp.Scene* scene,
        Dictionary<string, nint> nodesByName)
    {
        // Collect bone names from mesh weights; inverse-bind from node bind pose (row-vector).
        var boneNames = new HashSet<string>(StringComparer.Ordinal);
        for (uint mi = 0; mi < scene->MNumMeshes; mi++)
        {
            var aiMesh = scene->MMeshes[mi];
            if (aiMesh->MNumBones == 0 || aiMesh->MBones == null)
                continue;

            for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
            {
                var name = aiMesh->MBones[bi]->MName.AsString;
                if (!string.IsNullOrEmpty(name))
                    boneNames.Add(name);
            }
        }

        if (boneNames.Count == 0)
            throw new InvalidOperationException("Skinned import found no bones (mBones == 0).");

        // Joints that animate but have no weights.
        for (uint ai = 0; ai < scene->MNumAnimations; ai++)
        {
            var anim = scene->MAnimations[ai];
            for (uint ci = 0; ci < anim->MNumChannels; ci++)
            {
                var nodeName = anim->MChannels[ci]->MNodeName.AsString;
                if (!string.IsNullOrEmpty(nodeName))
                    boneNames.Add(nodeName);
            }
        }

        if (boneNames.Count > MaxSkeletonBones)
            throw new InvalidOperationException(
                $"Skeleton has {boneNames.Count} bones; maximum is {MaxSkeletonBones}.");

        var sortedNames = boneNames.ToList();
        sortedNames.Sort((a, b) =>
        {
            var da = NodeDepth(a, nodesByName);
            var db = NodeDepth(b, nodesByName);
            var cmp = da.CompareTo(db);
            return cmp != 0 ? cmp : string.CompareOrdinal(a, b);
        });

        var indexByName = new Dictionary<string, int>(sortedNames.Count, StringComparer.Ordinal);
        for (var i = 0; i < sortedNames.Count; i++)
            indexByName[sortedNames[i]] = i;

        var bones = new List<SkeletonBone>(sortedNames.Count);
        foreach (var name in sortedNames)
        {
            var parentIndex = -1;
            nint nodePtr = 0;
            if (nodesByName.TryGetValue(name, out nodePtr))
            {
                var node = (Node*)nodePtr;
                for (var parent = node->MParent; parent != null; parent = parent->MParent)
                {
                    var parentName = parent->MName.AsString;
                    if (indexByName.TryGetValue(parentName, out var pi))
                    {
                        parentIndex = pi;
                        break;
                    }
                }
            }

            var inverseBind = TryAssimpOffsetMatrix(scene, name);
            if (inverseBind == Matrix4x4.Identity)
            {
                Logger.Warning("Bone {Bone} missing Assimp offset matrix", name);
                if (nodePtr != 0)
                {
                    var bindGlobalRow = ComputeBindGlobalRow((Node*)nodePtr);
                    if (!Matrix4x4.Invert(bindGlobalRow, out inverseBind))
                        inverseBind = Matrix4x4.Identity;
                }
            }

            bones.Add(new SkeletonBone(name, parentIndex, inverseBind));
        }

        return new SkeletonAsset(bones);
    }

    private static unsafe Matrix4x4 ComputeBindGlobalRow(Node* node)
    {
        var depth = 0;
        for (var n = node; n != null; n = n->MParent)
            depth++;

        Span<nint> chain = stackalloc nint[depth];
        var index = depth - 1;
        for (var n = node; n != null; n = n->MParent)
            chain[index--] = (nint)n;

        var worldColumn = Matrix4x4.Identity;
        for (var i = 0; i < depth; i++)
        {
            var n = (Node*)chain[i];
            worldColumn = Matrix4x4.Multiply(worldColumn, n->MTransformation);
        }

        return Matrix4x4.Transpose(worldColumn);
    }

    private static unsafe Matrix4x4 TryAssimpOffsetMatrix(Silk.NET.Assimp.Scene* scene, string boneName)
    {
        for (uint mi = 0; mi < scene->MNumMeshes; mi++)
        {
            var aiMesh = scene->MMeshes[mi];
            if (aiMesh->MNumBones == 0 || aiMesh->MBones == null)
                continue;

            for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
            {
                var bone = aiMesh->MBones[bi];
                if (!string.Equals(bone->MName.AsString, boneName, StringComparison.Ordinal))
                    continue;

                return Matrix4x4.Transpose(bone->MOffsetMatrix);
            }
        }

        return Matrix4x4.Identity;
    }

    private static int NodeDepth(string name, Dictionary<string, nint> nodesByName)
    {
        if (!nodesByName.TryGetValue(name, out var ptr))
            return int.MaxValue;

        unsafe
        {
            var depth = 0;
            for (var n = ((Node*)ptr)->MParent; n != null; n = n->MParent)
                depth++;
            return depth;
        }
    }

    private static unsafe Anim3dAsset ExtractAnimations(
        Silk.NET.Assimp.Scene* scene,
        Dictionary<string, int> boneIndexByName)
    {
        var clips = new List<Anim3dClip>((int)scene->MNumAnimations);
        for (uint ai = 0; ai < scene->MNumAnimations; ai++)
        {
            var anim = scene->MAnimations[ai];
            var ticksPerSecond = anim->MTicksPerSecond > 0 ? anim->MTicksPerSecond : 25.0;
            var durationSeconds = (float)(anim->MDuration / ticksPerSecond);
            var name = anim->MName.AsString;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Clip{ai}";

            var channels = new List<Anim3dChannel>((int)anim->MNumChannels);
            for (uint ci = 0; ci < anim->MNumChannels; ci++)
            {
                var channel = anim->MChannels[ci];
                var nodeName = channel->MNodeName.AsString;
                if (!boneIndexByName.TryGetValue(nodeName, out var boneIndex))
                    continue;

                var translations = new List<Anim3dVec3Key>((int)channel->MNumPositionKeys);
                for (uint k = 0; k < channel->MNumPositionKeys; k++)
                {
                    var key = channel->MPositionKeys[k];
                    translations.Add(new Anim3dVec3Key((float)(key.MTime / ticksPerSecond), key.MValue));
                }

                var rotations = new List<Anim3dQuatKey>((int)channel->MNumRotationKeys);
                for (uint k = 0; k < channel->MNumRotationKeys; k++)
                {
                    var key = channel->MRotationKeys[k];
                    var q = key.MValue;
                    rotations.Add(new Anim3dQuatKey(
                        (float)(key.MTime / ticksPerSecond),
                        new Quaternion(q.X, q.Y, q.Z, q.W)));
                }

                var scales = new List<Anim3dVec3Key>((int)channel->MNumScalingKeys);
                for (uint k = 0; k < channel->MNumScalingKeys; k++)
                {
                    var key = channel->MScalingKeys[k];
                    scales.Add(new Anim3dVec3Key((float)(key.MTime / ticksPerSecond), key.MValue));
                }

                channels.Add(new Anim3dChannel((uint)boneIndex, translations, rotations, scales));
            }

            clips.Add(new Anim3dClip(name, durationSeconds, channels));
        }

        return new Anim3dAsset(clips);
    }

    private static unsafe Mesh ExtractSkinnedMesh(
        Silk.NET.Assimp.Mesh* aiMesh,
        Dictionary<string, int> boneIndexByName)
    {
        var mesh = ExtractMesh(aiMesh);
        if (aiMesh->MNumBones == 0 || aiMesh->MBones == null)
            return mesh;

        var vertCount = mesh.Vertices.Count;
        var influences = new List<(int Bone, float Weight)>[vertCount];
        for (var i = 0; i < vertCount; i++)
            influences[i] = [];

        for (uint bi = 0; bi < aiMesh->MNumBones; bi++)
        {
            var bone = aiMesh->MBones[bi];
            var name = bone->MName.AsString;
            if (!boneIndexByName.TryGetValue(name, out var boneIndex))
                continue;

            for (uint wi = 0; wi < bone->MNumWeights; wi++)
            {
                var vw = bone->MWeights[wi];
                var vid = (int)vw.MVertexId;
                if (vid < 0 || vid >= vertCount)
                    continue;
                influences[vid].Add((boneIndex, vw.MWeight));
            }
        }

        for (var vi = 0; vi < vertCount; vi++)
        {
            var list = influences[vi];
            if (list.Count == 0)
                continue;

            // LimitBoneWeights caps Assimp side; still keep ≤4 and renormalize.
            list.Sort((a, b) => b.Weight.CompareTo(a.Weight));
            if (list.Count > 4)
                list.RemoveRange(4, list.Count - 4);

            var sum = 0f;
            for (var i = 0; i < list.Count; i++)
                sum += list[i].Weight;
            if (sum > 1e-6f)
            {
                for (var i = 0; i < list.Count; i++)
                    list[i] = (list[i].Bone, list[i].Weight / sum);
            }

            var idx = new int[4];
            var wgt = new float[4];
            for (var i = 0; i < list.Count; i++)
            {
                idx[i] = list[i].Bone;
                wgt[i] = list[i].Weight;
            }

            var v = mesh.Vertices[vi];
            mesh.Vertices[vi] = v with
            {
                BoneIndex = new Vector4(idx[0], idx[1], idx[2], idx[3]),
                BoneWeight = new Vector4(wgt[0], wgt[1], wgt[2], wgt[3])
            };
        }

        return mesh;
    }

    private static unsafe Mesh ExtractMesh(Silk.NET.Assimp.Mesh* aiMesh)
    {
        var mesh = new Mesh(aiMesh->MName.AsString);

        var hasTexCoords = aiMesh->MTextureCoords[0] != null;
        var hasTangents = aiMesh->MTangents != null;
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);

        for (uint i = 0; i < aiMesh->MNumVertices; i++)
        {
            var vertex = new Mesh.Vertex
            {
                Position = aiMesh->MVertices[i]
            };

            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);

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

        var extent = max - min;
        Logger.Debug(
            "Mesh extracted name={Name} verts={Verts} indices={Indices} hasUV={HasUV} hasNormals={HasNormals} boundsMin={Min} boundsMax={Max} extent={Extent}",
            mesh.Name, mesh.Vertices.Count, mesh.Indices.Count, hasTexCoords, aiMesh->MNormals != null, min, max, extent);

        return mesh;
    }

    private unsafe MeshMaterial ExtractMaterial(
        Silk.NET.Assimp.Scene* scene,
        uint materialIndex,
        string directory)
    {
        var aiMaterial = scene->MMaterials[materialIndex];

        var albedoPath = ResolveTexturePath(scene, aiMaterial, TextureType.BaseColor, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Diffuse, directory);

        // assimp 5.x exposes the glTF combined metallic-roughness map as aiTextureType_UNKNOWN.
        var mrPath = ResolveTexturePath(scene, aiMaterial, TextureType.Unknown, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Metalness, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.DiffuseRoughness, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Specular, directory);

        var normalPath = ResolveTexturePath(scene, aiMaterial, TextureType.Normals, directory)
            ?? ResolveTexturePath(scene, aiMaterial, TextureType.Height, directory);

        var material = new MeshMaterial
        {
            AlbedoTexturePath = albedoPath,
            MetallicRoughnessTexturePath = mrPath,
            NormalTexturePath = normalPath
        };

        material.Metallic = TryGetFloat(aiMaterial, Assimp.MatkeyMetallicFactor, out var metallic)
            ? System.Math.Clamp(metallic, 0f, 1f)
            : 0f;

        if (TryGetFloat(aiMaterial, Assimp.MatkeyRoughnessFactor, out var roughness))
            material.Roughness = System.Math.Clamp(roughness, 0f, 1f);
        else if (mrPath == null &&
                 TryGetFloat(aiMaterial, Assimp.MaterialShininess, out var shininess) &&
                 shininess > 0f)
            material.Roughness = System.Math.Clamp(1f - shininess / 256f, 0.04f, 1f);
        else
            material.Roughness = 0.5f;

        // glTF default metallicFactor is 1.0. Albedo-only assets that keep that default
        // render near-black under our no-IBL PBR path — treat as dielectric when no MR map.
        if (mrPath == null && material.Metallic >= 0.99f)
        {
            Logger.Warning(
                "Material[{Index}] metallic=1 with no metallic-roughness map — " +
                "defaulting metallic to 0 (glTF default is unlit without IBL). " +
                "Use Metallic Override if this should stay metal.",
                materialIndex);
            material.Metallic = 0f;
        }

        var hasBaseColor = TryGetColor(aiMaterial, Assimp.MatkeyBaseColor, out var baseColor);
        var hasDiffuse = TryGetColor(aiMaterial, Assimp.MatkeyColorDiffuse, out var diffuseColor);
        Logger.Debug(
            "Material[{Index}] albedoPath={Albedo} mrPath={MR} normalPath={Normal} metallic={Metallic} roughness={Roughness} baseColor={HasBase}:{BaseColor} diffuseColor={HasDiff}:{DiffuseColor}",
            materialIndex,
            albedoPath ?? "<null>",
            mrPath ?? "<null>",
            normalPath ?? "<null>",
            material.Metallic,
            material.Roughness,
            hasBaseColor, baseColor,
            hasDiffuse, diffuseColor);

        return material;
    }

    private unsafe bool TryGetFloat(Silk.NET.Assimp.Material* aiMaterial, string key, out float value)
    {
        value = 0f;
        var result = assimp.GetMaterialFloatArray(aiMaterial, key, 0, 0, ref value, (uint*)null);
        return result == Return.Success;
    }

    private unsafe bool TryGetColor(Silk.NET.Assimp.Material* aiMaterial, string key, out Vector4 color)
    {
        color = default;
        var result = assimp.GetMaterialColor(aiMaterial, key, 0, 0, ref color);
        return result == Return.Success;
    }

    private unsafe string? ResolveTexturePath(
        Silk.NET.Assimp.Scene* scene,
        Silk.NET.Assimp.Material* aiMaterial,
        TextureType textureType,
        string directory)
    {
        var slotCount = assimp.GetMaterialTextureCount(aiMaterial, textureType);
        if (slotCount == 0)
            return null;

        AssimpString aiPath;
        var result = assimp.GetMaterialTexture(aiMaterial, textureType, 0, &aiPath, null, null, null, null, null, null);
        if (result != Return.Success)
        {
            Logger.Warning("GetMaterialTexture failed type={Type} result={Result}", textureType, result);
            return null;
        }

        var texturePath = aiPath.AsString;
        if (string.IsNullOrEmpty(texturePath))
        {
            Logger.Warning("Texture type={Type} has empty path (count={Count})", textureType, slotCount);
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

    private unsafe string? ExtractEmbeddedTextureToCache(Silk.NET.Assimp.Scene* scene, string embeddedRef)
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

        // Compressed image blob (png/jpg/…). Uncompressed texel buffers are rare for glTF/GLB.
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
        // ponytail: content-addressed temp cache; switch to CreateFromMemory if temp files become a problem
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
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
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
}
