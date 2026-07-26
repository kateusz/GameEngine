using Editor.Features.Import;
using Editor.UI.Drawers;
using Engine.Core;
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
    public void DestinationMeshPath_BuildsFlatModelsLayout()
    {
        var source = Path.Combine(_root, "MyModel.FBX");
        var dest = Import3DModelBatch.DestinationMeshPath(_assets, source);

        dest.ShouldBe(Path.Combine(_assets, "models", "MyModel.mesh"));
    }

    [Fact]
    public void TryImportBatch_ConflictsWithoutConfirm_AbortsWithoutTouchingDestination()
    {
        var source = Path.Combine(_root, "prop.glb");
        File.WriteAllText(source, "x");
        var dest = Import3DModelBatch.DestinationMeshPath(_assets, source);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
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
        File.Exists(Path.Combine(_assets, "models", "ok.mesh")).ShouldBeTrue();
    }

    [Fact]
    public void GuardNoProject_And_SummaryMessageType()
    {
        Import3DModelBatch.NoProjectError.ShouldNotBeNullOrWhiteSpace();

        Import3DModelBatch.SummaryMessageType(succeeded: 2, failed: 0).ShouldBe(MessageType.Success);
        Import3DModelBatch.SummaryMessageType(succeeded: 2, failed: 1).ShouldBe(MessageType.Warning);
        Import3DModelBatch.SummaryMessageType(succeeded: 0, failed: 1).ShouldBe(MessageType.Error);
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
}
