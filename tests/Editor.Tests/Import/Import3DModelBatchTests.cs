using Editor.Features.Import;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Renderer;
using Engine.Renderer.Skeletal;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Import;

public class Import3DModelBatchTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "Import3DModelBatchTests_" + Guid.NewGuid().ToString("N"));
    private readonly string _assets;
    private readonly string _sourceDir;

    public Import3DModelBatchTests()
    {
        _assets = Path.Combine(_root, "assets");
        _sourceDir = Path.Combine(_root, "src");
        Directory.CreateDirectory(_assets);
        Directory.CreateDirectory(_sourceDir);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assets);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ponytail: best-effort temp cleanup
        }
    }

    [Fact]
    public void EnumerateSources_Folder_ReturnsOnlyAllowlistedExtensions_NonRecursive()
    {
        var folder = Path.Combine(_root, "enum");
        Directory.CreateDirectory(folder);
        Directory.CreateDirectory(Path.Combine(folder, "nested"));
        File.WriteAllText(Path.Combine(folder, "a.fbx"), "x");
        File.WriteAllText(Path.Combine(folder, "b.glb"), "x");
        File.WriteAllText(Path.Combine(folder, "c.gltf"), "x");
        File.WriteAllText(Path.Combine(folder, "d.obj"), "x");
        File.WriteAllText(Path.Combine(folder, "e.txt"), "x");
        File.WriteAllText(Path.Combine(folder, "nested", "deep.fbx"), "x");

        var sources = Import3DModelBatch.EnumerateSources(folder);

        sources.Count.ShouldBe(3);
        sources.Select(Path.GetFileName).OrderBy(x => x).ShouldBe(["a.fbx", "b.glb", "c.gltf"]);
    }

    [Fact]
    public void EnumerateSources_SingleSupportedFile_ReturnsThatFile()
    {
        var file = Path.Combine(_root, "hero.glb");
        File.WriteAllText(file, "x");

        var sources = Import3DModelBatch.EnumerateSources(file);

        sources.ShouldHaveSingleItem().ShouldBe(Path.GetFullPath(file));
    }

    [Fact]
    public void EnumerateSources_UnsupportedSingleFile_ReturnsEmpty()
    {
        var file = Path.Combine(_root, "hero.obj");
        File.WriteAllText(file, "x");

        Import3DModelBatch.EnumerateSources(file).ShouldBeEmpty();
    }

    [Fact]
    public void TryImportBatch_EmptySources_SucceedsWithZeroCounts()
    {
        var ok = Import3DModelBatch.TryImportBatch(
            [],
            _assets,
            overwriteConfirmed: false,
            out var summary);

        ok.ShouldBeTrue();
        summary.ShouldNotBeNull();
        var empty = summary!.Value;
        empty.Succeeded.ShouldBe(0);
        empty.Failures.ShouldBeEmpty();
    }

    [Fact]
    public void FormatOverwriteMessage_MentionsMeshDestinations()
    {
        var msg = Import3DModelBatch.FormatOverwriteMessage(2);

        msg.ShouldContain("2");
        msg.ShouldContain(".mesh");
        msg.ShouldContain("Overwrite");
    }

    [Fact]
    public void FindDuplicateDestinations_SameStemDifferentExtensions_ReportsConflict()
    {
        var fbx = Path.Combine(_sourceDir, "robot.fbx");
        var glb = Path.Combine(_sourceDir, "robot.glb");
        File.WriteAllText(fbx, "x");
        File.WriteAllText(glb, "x");

        var dupes = Import3DModelBatch.FindDuplicateDestinations([fbx, glb]);

        dupes.ShouldHaveSingleItem();
        dupes[0].Stem.ShouldBe("robot");
        dupes[0].Sources.Select(Path.GetFileName).OrderBy(x => x)
            .ShouldBe(["robot.fbx", "robot.glb"]);

        var msg = Import3DModelBatch.FormatDuplicateDestinationMessage(dupes);
        msg.ShouldContain("robot.mesh");
        msg.ShouldContain("robot.fbx");
        msg.ShouldContain("robot.glb");
    }

    [Fact]
    public void TryImportBatch_DuplicateDestinations_AbortsWithoutCooking()
    {
        var fbx = Path.Combine(_sourceDir, "robot.fbx");
        var glb = Path.Combine(_sourceDir, "robot.glb");
        File.WriteAllText(fbx, "x");
        File.WriteAllText(glb, "x");

        var ok = Import3DModelBatch.TryImportBatch(
            [fbx, glb],
            _assets,
            overwriteConfirmed: true,
            out var summary);

        ok.ShouldBeTrue();
        summary.ShouldNotBeNull();
        var result = summary!.Value;
        result.Succeeded.ShouldBe(0);
        result.Failures.ShouldHaveSingleItem();
        result.Failures[0].Error.ShouldContain("robot.mesh");
        Directory.Exists(Path.Combine(_assets, "models")).ShouldBeFalse();
    }

    [Fact]
    public void TryImportBatch_ConflictsWithoutConfirm_AbortsWithoutTouchingDestination()
    {
        var source = Path.Combine(_root, "prop.glb");
        File.WriteAllText(source, "x");
        var models = Path.Combine(_assets, "models");
        Directory.CreateDirectory(models);
        var dest = Path.Combine(models, "prop.mesh");
        File.WriteAllText(dest, "existing");

        var aborted = !Import3DModelBatch.TryImportBatch(
            [source],
            _assets,
            overwriteConfirmed: false,
            out var summary);

        aborted.ShouldBeTrue();
        summary.ShouldBeNull();
        File.ReadAllText(dest).ShouldBe("existing");
        Import3DModelBatch.CountExistingDestinations([source], _assets).ShouldBe(1);
    }

    [Fact]
    public void TryImportBatch_ContinueOnFail_AggregatesOkFailCountsAndReasons()
    {
        var okSrc = WriteObjTriangle(_sourceDir, "ok");
        var badSrc = Path.Combine(_sourceDir, "bad.glb");
        File.WriteAllText(badSrc, "not-a-model");

        var ok = Import3DModelBatch.TryImportBatch(
            [okSrc, badSrc],
            _assets,
            overwriteConfirmed: true,
            out var summary);

        ok.ShouldBeTrue();
        summary.ShouldNotBeNull();
        var result = summary!.Value;
        result.Succeeded.ShouldBe(1);
        result.Failures.ShouldHaveSingleItem();
        result.Failures[0].Source.ShouldBe(Path.GetFullPath(badSrc));
        result.Failures[0].Error.ShouldNotBeNullOrWhiteSpace();
        result.Sources.First().Parts.Count.ShouldBeGreaterThan(0);
        File.Exists(Path.Combine(_assets, "models", "ok.mesh")).ShouldBeTrue();
        Directory.GetFiles(Path.Combine(_assets, "models"), "*.mesh").Length.ShouldBe(1);
    }

    [Fact]
    public void GuardNoProject_And_SummaryMessageType()
    {
        Import3DModelBatch.NoProjectError.ShouldNotBeNullOrWhiteSpace();

        Import3DModelBatch.SummaryMessageType(succeeded: 2, failed: 0).ShouldBe(MessageType.Success);
        Import3DModelBatch.SummaryMessageType(succeeded: 2, failed: 1).ShouldBe(MessageType.Warning);
        Import3DModelBatch.SummaryMessageType(succeeded: 0, failed: 1).ShouldBe(MessageType.Error);
    }

    [Fact]
    public void TryImportBatch_SkinnedSource_UsesCreateSkinnedCompanions()
    {
        var source = WriteTwoBoneSkinnedGltf(_sourceDir, "hero");

        var ok = Import3DModelBatch.TryImportBatch(
            [source],
            _assets,
            overwriteConfirmed: true,
            out var summary);

        ok.ShouldBeTrue();
        summary.ShouldNotBeNull();
        var result = summary!.Value;
        result.Succeeded.ShouldBe(1);
        result.Failures.ShouldBeEmpty();
        result.Sources.ShouldHaveSingleItem();
        var imported = result.Sources[0];
        imported.SkeletonRelativePath.ShouldBe("models/hero.skel");
        imported.ClipRelativePath.ShouldBe("models/hero.anim3d");
        imported.Parts.Count.ShouldBeGreaterThan(0);

        File.Exists(Path.Combine(_assets, "models", "hero.mesh")).ShouldBeTrue();
        File.Exists(Path.Combine(_assets, "models", "hero.skel")).ShouldBeTrue();
        File.Exists(Path.Combine(_assets, "models", "hero.anim3d")).ShouldBeTrue();
    }

    [Fact]
    public void EvictImportedAssets_EvictsMeshSkeletonAndAnimPaths()
    {
        var models = Substitute.For<IModelFactory>();
        var skeletons = Substitute.For<ISkeletonFactory>();
        var anims = Substitute.For<IAnim3dFactory>();
        var source = new Import3DModelBatch.SourceImport(
            "/tmp/hero.fbx",
            [new MeshCreator.SplitPart("hero", "models/hero.mesh", 0, 1, default, default, default)],
            "models/hero.skel",
            "models/hero.anim3d");

        Import3DModelBatch.EvictImportedAssets(models, skeletons, anims, source);

        models.Received(1).Evict(PathBuilder.Resolve("models/hero.mesh"));
        skeletons.Received(1).Evict(PathBuilder.Resolve("models/hero.skel"));
        anims.Received(1).Evict(PathBuilder.Resolve("models/hero.anim3d"));
    }

    private static string WriteObjTriangle(string dir, string stem)
    {
        var path = Path.Combine(dir, $"{stem}.obj");
        File.WriteAllText(path, """
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vn 0 0 1
            f 1//1 2//1 3//1
            """);
        return path;
    }

    /// <summary>Minimal two-joint skinned triangle (mirrors Engine.Tests SkinnedGltfFixture).</summary>
    private static string WriteTwoBoneSkinnedGltf(string dir, string stem)
    {
        Directory.CreateDirectory(dir);
        var binPath = Path.Combine(dir, $"{stem}.bin");
        var gltfPath = Path.Combine(dir, $"{stem}.gltf");

        using (var stream = File.Create(binPath))
        using (var w = new BinaryWriter(stream))
        {
            void F3(float x, float y, float z) { w.Write(x); w.Write(y); w.Write(z); }
            void F4(float x, float y, float z, float ww) { w.Write(x); w.Write(y); w.Write(z); w.Write(ww); }
            void Identity()
            {
                for (var col = 0; col < 4; col++)
                for (var row = 0; row < 4; row++)
                    w.Write(row == col ? 1f : 0f);
            }

            F3(0, 0, 0); F3(1, 0, 0); F3(0, 1, 0);
            for (var i = 0; i < 3; i++) { w.Write((byte)0); w.Write((byte)1); w.Write((byte)0); w.Write((byte)0); }
            for (var i = 0; i < 3; i++) F4(0.75f, 0.25f, 0f, 0f);
            w.Write((ushort)0); w.Write((ushort)1); w.Write((ushort)2); w.Write((ushort)0);
            Identity(); Identity();
            w.Write(0f); w.Write(1f);
            F3(0, 0, 0); F3(0, 0.5f, 0);
        }

        var binLen = new FileInfo(binPath).Length;
        var doc = new Dictionary<string, object?>
        {
            ["asset"] = new { version = "2.0" },
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = new object[]
            {
                new { name = "Root", children = new[] { 1, 2 } },
                new { name = "Bone0", translation = new[] { 0f, 0f, 0f }, children = new[] { 3 } },
                new { name = "MeshNode", mesh = 0, skin = 0 },
                new { name = "Bone1", translation = new[] { 0f, 1f, 0f } }
            },
            ["meshes"] = new[]
            {
                new
                {
                    name = "SkinnedTri",
                    primitives = new[]
                    {
                        new
                        {
                            attributes = new Dictionary<string, int>
                            {
                                ["POSITION"] = 0, ["JOINTS_0"] = 1, ["WEIGHTS_0"] = 2
                            },
                            indices = 3
                        }
                    }
                }
            },
            ["skins"] = new[] { new { name = "Armature", joints = new[] { 1, 3 }, inverseBindMatrices = 4 } },
            ["animations"] = new[]
            {
                new
                {
                    name = "ClipA",
                    channels = new[] { new { sampler = 0, target = new { node = 1, path = "translation" } } },
                    samplers = new[] { new { input = 5, output = 6, interpolation = "LINEAR" } }
                }
            },
            ["buffers"] = new[] { new { uri = $"{stem}.bin", byteLength = binLen } },
            ["bufferViews"] = new object[]
            {
                new { buffer = 0, byteOffset = 0, byteLength = 36 },
                new { buffer = 0, byteOffset = 36, byteLength = 12 },
                new { buffer = 0, byteOffset = 48, byteLength = 48 },
                new { buffer = 0, byteOffset = 96, byteLength = 6 },
                new { buffer = 0, byteOffset = 104, byteLength = 128 },
                new { buffer = 0, byteOffset = 232, byteLength = 8 },
                new { buffer = 0, byteOffset = 240, byteLength = 24 }
            },
            ["accessors"] = new object[]
            {
                new
                {
                    bufferView = 0, componentType = 5126, count = 3, type = "VEC3",
                    max = new[] { 1f, 1f, 0f }, min = new[] { 0f, 0f, 0f }
                },
                new { bufferView = 1, componentType = 5121, count = 3, type = "VEC4" },
                new { bufferView = 2, componentType = 5126, count = 3, type = "VEC4" },
                new { bufferView = 3, componentType = 5123, count = 3, type = "SCALAR" },
                new { bufferView = 4, componentType = 5126, count = 2, type = "MAT4" },
                new
                {
                    bufferView = 5, componentType = 5126, count = 2, type = "SCALAR",
                    max = new[] { 1f }, min = new[] { 0f }
                },
                new { bufferView = 6, componentType = 5126, count = 2, type = "VEC3" }
            }
        };

        File.WriteAllText(gltfPath, System.Text.Json.JsonSerializer.Serialize(doc));
        return gltfPath;
    }
}
