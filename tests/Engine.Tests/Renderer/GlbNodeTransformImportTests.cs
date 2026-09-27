using System.Numerics;
using Engine.Renderer.Models;
using Engine.Renderer.Models.RuntimeMesh;
using Math;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class GlbNodeTransformImportTests
{
    [Fact]
    public void ImportSource_GltfNodeTranslation_LandsInRowTranslation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TranslatedBox.gltf");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine("tests", "Engine.Tests", "TestAssets", "TranslatedBox.gltf"));

        using var importer = new AssimpModelImporter();
        var model = importer.ImportSource(path);
        model.ShouldNotBeNull();

        var box = model!.Nodes.Single(n => n.Name == "Box");
        var m = box.LocalTransform;
        var dump =
            $"M14={m.M14} M24={m.M24} M34={m.M34} M41={m.M41} M42={m.M42} M43={m.M43}";

        MathHelpers.DecomposeTransform(m, out var translation, out _, out _).ShouldBeTrue(dump);
        translation.X.ShouldBe(3f, 0.001f, dump);
        translation.Y.ShouldBe(2f, 0.001f, dump);
        translation.Z.ShouldBe(1f, 0.001f, dump);

        var spun = model.Nodes.Single(n => n.Name == "Spun");
        var spunMatrix = spun.LocalTransform;
        var spunDump =
            $"M14={spunMatrix.M14} M24={spunMatrix.M24} M34={spunMatrix.M34} M41={spunMatrix.M41} M42={spunMatrix.M42} M43={spunMatrix.M43}";
        MathHelpers.DecomposeTransform(spunMatrix, out var spunTranslation, out _, out _).ShouldBeTrue(spunDump);
        spunTranslation.X.ShouldBe(4f, 0.01f, spunDump);
        spunTranslation.Y.ShouldBe(0f, 0.01f, spunDump);
        spunTranslation.Z.ShouldBe(0f, 0.01f, spunDump);
    }

    [Fact]
    public void RuntimeMeshRoundTrip_KeepsGltfNodeTranslation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "TranslatedBox.gltf");
        using var importer = new AssimpModelImporter();
        var imported = importer.ImportSource(path);
        imported.ShouldNotBeNull();

        var stamp = new RuntimeMeshStamp(1, new byte[32], RuntimeMeshFormat.ImporterVersion,
            RuntimeMeshFormat.AssimpPostProcessFlags);
        var bytes = RuntimeMeshWriter.Serialize(imported!, stamp);
        RuntimeMeshReader.TryRead(bytes, out var read).ShouldBeTrue();

        var graph = RuntimeMeshSceneGraph.Unflatten(read!.Nodes, read.Lights);
        graph.ShouldNotBeNull();
        var box = Find(graph!, "Box");
        box.ShouldNotBeNull();
        MathHelpers.DecomposeTransform(box!.LocalTransform, out var translation, out _, out _).ShouldBeTrue();
        translation.ShouldBe(new Vector3(3f, 2f, 1f));
    }

    [Fact]
    public void ImportSource_GltfTexCoord_MirrorsVForFlippedBitmap()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestAssets", "GltfUv.gltf");
        if (!File.Exists(path))
            path = Path.GetFullPath(Path.Combine("tests", "Engine.Tests", "TestAssets", "GltfUv.gltf"));

        using var importer = new AssimpModelImporter();
        var model = importer.ImportSource(path);
        model.ShouldNotBeNull();

        var uv = model!.Submeshes.Single().Vertices.Select(v => v.TexCoord).ToArray();
        uv[0].X.ShouldBe(0.2f, 0.001f);
        uv[0].Y.ShouldBe(0.7f, 0.001f);
        uv[1].Y.ShouldBe(1f - 2.3f, 0.001f);
        uv[2].Y.ShouldBe(1f, 0.001f);
    }

    private static ModelSceneNode? Find(ModelSceneNode node, string name)
    {
        if (node.Name == name)
            return node;
        foreach (var child in node.Children)
        {
            var found = Find(child, name);
            if (found != null)
                return found;
        }

        return null;
    }
}
