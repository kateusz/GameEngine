using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
using Math;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;
using Silk.NET.Assimp;

namespace Engine.Tests.Components;

public class ModelRendererComponentTests
{
    [Fact]
    public void ModelRendererComponent_DefaultConstructor_ShouldInitializeWithDefaults()
    {
        var component = new ModelRendererComponent();

        component.Color.ShouldBe(Vector4.One);
        component.ModelPath.ShouldBeNull();
        component.MetallicOverride.ShouldBeNull();
        component.RoughnessOverride.ShouldBeNull();
    }

    [Fact]
    public void ModelRendererComponent_Clone_ShouldCopyAllProperties()
    {
        var original = new ModelRendererComponent(new Vector4(0.2f, 0.4f, 0.6f, 0.8f))
        {
            ModelPath = "models/crate.fbx",
            MetallicOverride = 0.75f,
            RoughnessOverride = 0.25f
        };

        var clone = (ModelRendererComponent)original.Clone();
        clone.Color = Vector4.Zero;
        clone.MetallicOverride = 0f;

        clone.ShouldNotBeSameAs(original);
        clone.ModelPath.ShouldBe("models/crate.fbx");
        clone.Color.ShouldBe(Vector4.Zero);
        clone.MetallicOverride.ShouldBe(0f);
        clone.RoughnessOverride.ShouldBe(0.25f);
        original.Color.ShouldBe(new Vector4(0.2f, 0.4f, 0.6f, 0.8f));
        original.MetallicOverride.ShouldBe(0.75f);
    }
}

public class MeshMaterialTests
{
    [Fact]
    public void Defaults_ShouldBeDielectricMidRoughness()
    {
        var material = new MeshMaterial();

        material.Metallic.ShouldBe(0f);
        material.Roughness.ShouldBe(0.5f);
        material.BaseColorFactor.ShouldBe(Vector4.One);
        material.EmissiveFactor.ShouldBe(Vector3.Zero);
        material.AlphaMode.ShouldBe(MaterialAlphaMode.Opaque);
        material.AlphaCutoff.ShouldBe(0.5f);
        material.DoubleSided.ShouldBeFalse();
        material.HasAlbedoMap.ShouldBeFalse();
        material.HasMetallicRoughnessMap.ShouldBeFalse();
        material.HasNormalMap.ShouldBeFalse();
        material.HasEmissiveMap.ShouldBeFalse();
    }

    [Fact]
    public void ResolveBaseColor_MultipliesTintAndFactor()
    {
        var material = new MeshMaterial { BaseColorFactor = new Vector4(0.5f, 0.5f, 0.5f, 0.8f) };
        var tint = new Vector4(2f, 2f, 2f, 0.5f);

        material.ResolveBaseColor(tint).ShouldBe(new Vector4(1f, 1f, 1f, 0.4f));
    }
}

public class AssimpModelImporterTests : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();
    private readonly string _objPath;
    private readonly string _gltfPath;

    public AssimpModelImporterTests()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "TestAssets");
        Directory.CreateDirectory(assetsDir);
        _objPath = EnsureObjTriangle(assetsDir);
        _gltfPath = EnsureGltfPbrTriangle(assetsDir);
    }

    private static string EnsureObjTriangle(string assetsDir)
    {
        var path = Path.Combine(assetsDir, "triangle.obj");
        if (!System.IO.File.Exists(path))
        {
            System.IO.File.WriteAllText(path, """
                v 0 0 0
                v 1 0 0
                v 0 1 0
                vn 0 0 1
                f 1//1 2//1 3//1
                """);
        }

        return path;
    }

    private static string EnsureGltfPbrTriangle(string assetsDir)
    {
        var binPath = Path.Combine(assetsDir, "triangle.bin");
        using (var stream = System.IO.File.Create(binPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0f); writer.Write(0f); writer.Write(0f);
            writer.Write(1f); writer.Write(0f); writer.Write(0f);
            writer.Write(0f); writer.Write(1f); writer.Write(0f);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
        }

        var gltfPath = Path.Combine(assetsDir, "triangle_pbr.gltf");
        System.IO.File.WriteAllText(gltfPath, """
            {
              "asset": { "version": "2.0" },
              "scenes": [{ "nodes": [0] }],
              "nodes": [{ "mesh": 0 }],
              "meshes": [{
                "primitives": [{
                  "attributes": { "POSITION": 0 },
                  "indices": 1,
                  "material": 0
                }]
              }],
              "materials": [{
                "pbrMetallicRoughness": {
                  "baseColorFactor": [1, 0, 0, 1],
                  "metallicFactor": 0.8,
                  "roughnessFactor": 0.2
                }
              }],
              "buffers": [{ "uri": "triangle.bin", "byteLength": 42 }],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 36 },
                { "buffer": 0, "byteOffset": 36, "byteLength": 6 }
              ],
              "accessors": [
                {
                  "bufferView": 0,
                  "componentType": 5126,
                  "count": 3,
                  "type": "VEC3",
                  "max": [1, 1, 0],
                  "min": [0, 0, 0]
                },
                {
                  "bufferView": 1,
                  "componentType": 5123,
                  "count": 3,
                  "type": "SCALAR"
                }
              ]
            }
            """);

        return gltfPath;
    }

    private static string EnsureGltfWithEmbeddedAlbedo(string assetsDir)
    {
        var binPath = Path.Combine(assetsDir, "triangle_embedded.bin");
        using (var stream = System.IO.File.Create(binPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0f); writer.Write(0f); writer.Write(0f);
            writer.Write(1f); writer.Write(0f); writer.Write(0f);
            writer.Write(0f); writer.Write(1f); writer.Write(0f);
            writer.Write(0f); writer.Write(0f);
            writer.Write(1f); writer.Write(0f);
            writer.Write(0f); writer.Write(1f);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
        }

        // 1x1 white PNG as data-URI — Assimp promotes this to an embedded texture (*0).
        const string pngBase64 =
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        var gltfPath = Path.Combine(assetsDir, "triangle_embedded_albedo.gltf");
        System.IO.File.WriteAllText(gltfPath, $$"""
            {
              "asset": { "version": "2.0" },
              "scenes": [{ "nodes": [0] }],
              "nodes": [{ "mesh": 0 }],
              "meshes": [{
                "primitives": [{
                  "attributes": { "POSITION": 0, "TEXCOORD_0": 1 },
                  "indices": 2,
                  "material": 0
                }]
              }],
              "materials": [{
                "pbrMetallicRoughness": {
                  "baseColorTexture": { "index": 0 },
                  "metallicFactor": 1.0,
                  "roughnessFactor": 0.5
                }
              }],
              "textures": [{ "source": 0 }],
              "images": [{ "uri": "data:image/png;base64,{{pngBase64}}" }],
              "buffers": [{ "uri": "triangle_embedded.bin", "byteLength": 66 }],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 36 },
                { "buffer": 0, "byteOffset": 36, "byteLength": 24 },
                { "buffer": 0, "byteOffset": 60, "byteLength": 6 }
              ],
              "accessors": [
                {
                  "bufferView": 0,
                  "componentType": 5126,
                  "count": 3,
                  "type": "VEC3",
                  "max": [1, 1, 0],
                  "min": [0, 0, 0]
                },
                {
                  "bufferView": 1,
                  "componentType": 5126,
                  "count": 3,
                  "type": "VEC2"
                },
                {
                  "bufferView": 2,
                  "componentType": 5123,
                  "count": 3,
                  "type": "SCALAR"
                }
              ]
            }
            """);

        return gltfPath;
    }

    [Fact]
    public void Import_ObjTriangle_ShouldProduceDielectricMaterial()
    {
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(_objPath);

        submeshes.Count.ShouldBe(1);
        submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
        submeshes[0].Mesh.Indices.Count.ShouldBe(3);
        submeshes[0].Material.Metallic.ShouldBe(0f);
        submeshes[0].Material.Roughness.ShouldBeInRange(0.04f, 1f);
    }

    [Fact]
    public void Import_GltfWithEmbeddedAlbedo_ShouldResolveAlbedoPathToExistingFile()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "TestAssets");
        Directory.CreateDirectory(assetsDir);
        var gltfPath = EnsureGltfWithEmbeddedAlbedo(assetsDir);
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(gltfPath);

        submeshes.Count.ShouldBe(1);
        submeshes[0].Material.AlbedoTexturePath.ShouldNotBeNull();
        System.IO.File.Exists(submeshes[0].Material.AlbedoTexturePath!).ShouldBeTrue();
        Path.GetExtension(submeshes[0].Material.AlbedoTexturePath!).ShouldBe(".png");
        // glTF default metallic=1 with no MR map → forced dielectric for no-IBL shading
        submeshes[0].Material.Metallic.ShouldBe(0f);
    }

    [Fact]
    public void Import_GltfPbrTriangle_ShouldReadMetallicRoughnessFactors()
    {
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(_gltfPath);

        submeshes.Count.ShouldBe(1);
        submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
        submeshes[0].Material.Metallic.ShouldBe(0.8f, 0.01);
        submeshes[0].Material.Roughness.ShouldBe(0.2f, 0.01);
    }

    [Fact]
    public void ImportParts_GltfTranslatedNode_YieldsNumericsTranslation()
    {
        var assetsDir = Path.Combine(AppContext.BaseDirectory, "TestAssets");
        Directory.CreateDirectory(assetsDir);
        var gltfPath = EnsureGltfTranslatedTriangle(assetsDir);
        var importer = new AssimpModelImporter(_assimp);

        var parts = importer.ImportParts(gltfPath);

        parts.Count.ShouldBe(1);
        MathHelpers.DecomposeTransform(
            parts[0].LocalToRoot, out var translation, out _, out var scale);
        translation.X.ShouldBe(10f, 0.01f);
        translation.Y.ShouldBe(20f, 0.01f);
        translation.Z.ShouldBe(30f, 0.01f);
        scale.X.ShouldBe(1f, 0.01f);
        scale.Y.ShouldBe(1f, 0.01f);
        scale.Z.ShouldBe(1f, 0.01f);
    }

    private static string EnsureGltfTranslatedTriangle(string assetsDir)
    {
        var binPath = Path.Combine(assetsDir, "triangle_translated.bin");
        using (var stream = System.IO.File.Create(binPath))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0f); writer.Write(0f); writer.Write(0f);
            writer.Write(1f); writer.Write(0f); writer.Write(0f);
            writer.Write(0f); writer.Write(1f); writer.Write(0f);
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
        }

        var gltfPath = Path.Combine(assetsDir, "triangle_translated.gltf");
        System.IO.File.WriteAllText(gltfPath, """
            {
              "asset": { "version": "2.0" },
              "scenes": [{ "nodes": [0] }],
              "nodes": [{ "mesh": 0, "translation": [10, 20, 30] }],
              "meshes": [{
                "primitives": [{
                  "attributes": { "POSITION": 0 },
                  "indices": 1
                }]
              }],
              "buffers": [{ "uri": "triangle_translated.bin", "byteLength": 42 }],
              "bufferViews": [
                { "buffer": 0, "byteOffset": 0, "byteLength": 36 },
                { "buffer": 0, "byteOffset": 36, "byteLength": 6 }
              ],
              "accessors": [
                {
                  "bufferView": 0,
                  "componentType": 5126,
                  "count": 3,
                  "type": "VEC3",
                  "max": [1, 1, 0],
                  "min": [0, 0, 0]
                },
                {
                  "bufferView": 1,
                  "componentType": 5123,
                  "count": 3,
                  "type": "SCALAR"
                }
              ]
            }
            """);
        return gltfPath;
    }

    [Fact]
    public void Import_MissingFile_ShouldReturnEmpty()
    {
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj"));

        submeshes.ShouldBeEmpty();
    }

    public void Dispose() => _assimp.Dispose();
}

public class ModelFactoryTests
{
    [Fact]
    public void Create_MissingMeshPath_ShouldReturnNull()
    {
        var factory = CreateFactory();

        factory.Create(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.mesh"))
            .ShouldBeNull();
    }

    [Fact]
    public void Create_InvalidMeshPath_ShouldNotCacheSuccess()
    {
        var factory = CreateFactory();
        var missing = Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.mesh");

        factory.Create(missing).ShouldBeNull();
        factory.Create(missing).ShouldBeNull();
    }

    private static ModelFactory CreateFactory()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        textureFactory.GetWhiteTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetBlackTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetFlatNormalTexture().Returns(Substitute.For<Texture2D>());

        return new ModelFactory(
            textureFactory,
            Substitute.For<Engine.Renderer.Buffers.VertexArray.IVertexArrayFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IVertexBufferFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IIndexBufferFactory>());
    }
}
