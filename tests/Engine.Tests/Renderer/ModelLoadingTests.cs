using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
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
        material.HasAlbedoMap.ShouldBeFalse();
        material.HasMetallicRoughnessMap.ShouldBeFalse();
        material.HasNormalMap.ShouldBeFalse();
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
    public void Import_MissingFile_ShouldReturnEmpty()
    {
        var importer = new AssimpModelImporter(_assimp);

        var submeshes = importer.Import(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj"));

        submeshes.ShouldBeEmpty();
    }

    public void Dispose() => _assimp.Dispose();
}

public class ModelFactoryTests : IDisposable
{
    private readonly Assimp _assimp = Assimp.GetApi();

    [Fact]
    public void Create_MissingPath_ShouldReturnNull()
    {
        var factory = CreateFactory();

        factory.Create(Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj"))
            .ShouldBeNull();
    }

    [Fact]
    public void Create_InvalidPath_ShouldNotCacheSuccess()
    {
        var factory = CreateFactory();
        var missing = Path.Combine(AppContext.BaseDirectory, "TestAssets", "missing.obj");

        factory.Create(missing).ShouldBeNull();
        factory.Create(missing).ShouldBeNull();
    }

    private ModelFactory CreateFactory()
    {
        var textureFactory = Substitute.For<ITextureFactory>();
        textureFactory.GetWhiteTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetBlackTexture().Returns(Substitute.For<Texture2D>());
        textureFactory.GetFlatNormalTexture().Returns(Substitute.For<Texture2D>());

        return new ModelFactory(
            textureFactory,
            Substitute.For<Engine.Renderer.Buffers.VertexArray.IVertexArrayFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IVertexBufferFactory>(),
            Substitute.For<Engine.Renderer.Buffers.IIndexBufferFactory>(),
            _assimp);
    }

    public void Dispose() => _assimp.Dispose();
}
