using System.Numerics;
using Engine.Core;
using Engine.Renderer;
using Engine.Scene.Skeletal;
using Editor.Features.Import;
using NSubstitute;
using Shouldly;

namespace Editor.Tests.Import;

[Trait("Category", "Unit")]
[Collection("PathBuilder")]
public class MeshCreatorTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _assetsRoot;
    private readonly string _sourceDir;

    public MeshCreatorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "GameEngine-MeshCreatorTests", Guid.NewGuid().ToString("N"));
        _assetsRoot = Path.Combine(_tempRoot, "assets");
        _sourceDir = Path.Combine(_tempRoot, "source");
        Directory.CreateDirectory(_assetsRoot);
        Directory.CreateDirectory(_sourceDir);

        var context = Substitute.For<IProjectContext>();
        context.AssetsPath.Returns(_assetsRoot);
        PathBuilder.UseProjectContext(context);
    }

    public void Dispose()
    {
        PathBuilder.UseProjectContext(Substitute.For<IProjectContext>());
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // ponytail: temp cleanup best-effort
        }
    }

    [Fact]
    public void CreateSplit_ObjTriangle_WritesOneStemMeshWithSubmeshRanges()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "splitme");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        var result = MeshCreator.CreateSplit(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);
        result.Parts.Count.ShouldBeGreaterThan(0);

        var expectedPath = $"models/{stem}.mesh";
        File.Exists(Path.Combine(_assetsRoot, "models", $"{stem}.mesh")).ShouldBeTrue();
        Directory.GetFiles(Path.Combine(_assetsRoot, "models"), "*.mesh").Length.ShouldBe(1);

        var cursor = 0;
        foreach (var part in result.Parts)
        {
            part.MeshRelativePath.ShouldBe(expectedPath);
            part.SubmeshStart.ShouldBe(cursor);
            part.SubmeshCount.ShouldBeGreaterThan(0);
            cursor += part.SubmeshCount;
        }

        using (var stream = File.OpenRead(Path.Combine(_assetsRoot, "models", $"{stem}.mesh")))
            MeshReader.Read(stream).Submeshes.Count.ShouldBe(cursor);

        MeshCreator.CountExistingSplitMeshes(_assetsRoot, stem).ShouldBe(1);
    }

    [Fact]
    public void Create_ObjTriangle_WritesFlatModelsMesh()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "crate");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        var result = MeshCreator.Create(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);
        result.MeshRelativePath.ShouldBe($"models/{stem}.mesh");

        var meshAbsolute = Path.Combine(_assetsRoot, "models", $"{stem}.mesh");
        File.Exists(meshAbsolute).ShouldBeTrue();
        Directory.Exists(Path.Combine(_assetsRoot, "models", stem)).ShouldBeFalse();

        using var stream = File.OpenRead(meshAbsolute);
        var model = MeshReader.Read(stream);
        model.Submeshes.Count.ShouldBe(1);
        model.Submeshes[0].Mesh.Vertices.Count.ShouldBe(3);
    }

    [Fact]
    public void CreateSplit_SimpleSkinGltf_WritesSkeletonClipsAndIdentityParts()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "TestAssets", "SimpleSkin.gltf");
        File.Exists(sourcePath).ShouldBeTrue();

        var result = MeshCreator.CreateSplit(sourcePath, _assetsRoot, "simpleskin");

        result.Success.ShouldBeTrue(result.Error);
        result.Parts.Count.ShouldBeGreaterThan(0);
        foreach (var part in result.Parts)
        {
            part.Translation.ShouldBe(Vector3.Zero);
            part.Rotation.ShouldBe(Vector3.Zero);
            part.Scale.ShouldBe(Vector3.One);
        }

        using var stream = File.OpenRead(Path.Combine(_assetsRoot, "models", "simpleskin.mesh"));
        var model = MeshReader.Read(stream);
        model.HasSkeleton.ShouldBeTrue();
        model.Bones.Count.ShouldBeGreaterThan(0);
        model.Bones.Count.ShouldBeLessThanOrEqualTo(SkeletalLimits.MaxBones);
        model.Clips.Count.ShouldBeGreaterThan(0);

        var weighted = model.Submeshes.SelectMany(s => s.Mesh.Vertices).Count(v => v.Weights != Vector4.Zero);
        weighted.ShouldBeGreaterThan(0);

        var palette = SkeletalPoseMath.CreateIdentityPalette();
        SkeletalPoseMath.Evaluate(model.Bones, model.Clips[0], 0f, palette);
        var bind = SkeletalPoseMath.CreateIdentityPalette();
        SkeletalPoseMath.Evaluate(model.Bones, clip: null, 0f, bind);
        for (var i = 0; i < model.Bones.Count; i++)
        {
            palette[i].M11.ShouldBe(bind[i].M11, 1e-3f);
            palette[i].M42.ShouldBe(bind[i].M42, 1e-3f);
        }

        foreach (var channel in model.Clips[0].Channels)
        {
            float? prev = null;
            foreach (var key in channel.Rotations)
            {
                var len = key.Value.Length();
                len.ShouldBe(1f, 1e-3f);
                if (prev is not null)
                    key.Time.ShouldBeGreaterThan(prev.Value);
                prev = key.Time;
            }

            channel.Rotations.Count.ShouldBeGreaterThan(1);
        }
    }

    [Fact]
    public void Create_WithExternalTexture_CopiesTextureUnderModelsTextures()
    {
        var sourcePath = WriteObjWithAlbedo(_sourceDir, "textured");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        var result = MeshCreator.Create(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);

        var modelsDir = Path.Combine(_assetsRoot, "models");
        var texturesDir = Path.Combine(modelsDir, "textures");
        Directory.Exists(texturesDir).ShouldBeTrue();
        Directory.Exists(Path.Combine(modelsDir, stem)).ShouldBeFalse();
        var copiedTextures = Directory.GetFiles(texturesDir);
        copiedTextures.Length.ShouldBeGreaterThan(0);

        var embeddedCache = Path.Combine(Path.GetTempPath(), "GameEngine", "embedded-textures");
        foreach (var tex in copiedTextures)
        {
            Path.GetFullPath(tex).StartsWith(Path.GetFullPath(texturesDir), StringComparison.OrdinalIgnoreCase)
                .ShouldBeTrue();
            Path.GetFullPath(tex).StartsWith(Path.GetFullPath(embeddedCache), StringComparison.OrdinalIgnoreCase)
                .ShouldBeFalse();
        }

        using var stream = File.OpenRead(Path.Combine(modelsDir, $"{stem}.mesh"));
        var model = MeshReader.Read(stream);
        var albedo = model.Submeshes[0].Material.AlbedoTexturePath;
        albedo.ShouldNotBeNull();
        albedo.ShouldStartWith("models/textures/");
        Path.IsPathRooted(albedo!).ShouldBeFalse();
    }

    [Fact]
    public void Create_MaterialPaths_AreProjectRelativeUnderModelsTextures()
    {
        var sourcePath = WriteObjWithAlbedo(_sourceDir, "relpaths");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        var result = MeshCreator.Create(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);

        var meshAbsolute = Path.Combine(_assetsRoot, "models", $"{stem}.mesh");
        using var stream = File.OpenRead(meshAbsolute);
        var model = MeshReader.Read(stream);

        var albedo = model.Submeshes[0].Material.AlbedoTexturePath;
        albedo.ShouldNotBeNull();
        albedo.ShouldStartWith("models/textures/");
        Path.IsPathRooted(albedo).ShouldBeFalse();
        albedo.Contains(':').ShouldBeFalse(); // no Windows drive / Assimp abs
        File.Exists(PathBuilder.Resolve(albedo)).ShouldBeTrue();
    }

    [Fact]
    public void Create_MissingSource_ReturnsErrorResult()
    {
        var missing = Path.Combine(_sourceDir, "nope.obj");

        var result = MeshCreator.Create(missing, _assetsRoot, "nope");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        result.MeshRelativePath.ShouldBeNull();
        File.Exists(Path.Combine(_assetsRoot, "models", "nope.mesh")).ShouldBeFalse();
    }

    [Fact]
    public void Create_EmptyStem_ReturnsErrorResult()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "stemless");

        var result = MeshCreator.Create(sourcePath, _assetsRoot, " ");

        result.Success.ShouldBeFalse();
        result.Error.ShouldNotBeNullOrWhiteSpace();
        result.Error!.ShouldContain("Stem");
        result.MeshRelativePath.ShouldBeNull();
    }

    [Fact]
    public void Create_StemWithPathSeparator_ReturnsErrorResult()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "sepstem");

        var result = MeshCreator.Create(sourcePath, _assetsRoot, $"evil{Path.DirectorySeparatorChar}stem");

        result.Success.ShouldBeFalse();
        result.Error!.ShouldContain("Invalid stem");
        result.MeshRelativePath.ShouldBeNull();
    }

    [Fact]
    public void Create_Stem_DerivedFromSourceFilenameWithoutExtension()
    {
        var sourcePath = WriteObjTriangle(_sourceDir, "MyCoolModel");
        var stem = Path.GetFileNameWithoutExtension(sourcePath);

        stem.ShouldBe("MyCoolModel");

        var result = MeshCreator.Create(sourcePath, _assetsRoot, stem);

        result.Success.ShouldBeTrue(result.Error);
        result.MeshRelativePath.ShouldBe("models/MyCoolModel.mesh");
        File.Exists(Path.Combine(_assetsRoot, "models", "MyCoolModel.mesh")).ShouldBeTrue();
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

    private static string WriteObjWithAlbedo(string dir, string stem)
    {
        var modelDir = Path.Combine(dir, stem);
        Directory.CreateDirectory(modelDir);

        var pngPath = Path.Combine(modelDir, "albedo.png");
        File.WriteAllBytes(pngPath, Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg=="));

        File.WriteAllText(Path.Combine(modelDir, $"{stem}.mtl"), """
            newmtl Mat
            map_Kd albedo.png
            """);

        var objPath = Path.Combine(modelDir, $"{stem}.obj");
        File.WriteAllText(objPath, $"""
            mtllib {stem}.mtl
            v 0 0 0
            v 1 0 0
            v 0 1 0
            vt 0 0
            vt 1 0
            vt 0 1
            vn 0 0 1
            usemtl Mat
            f 1/1/1 2/2/1 3/3/1
            """);
        return objPath;
    }
}
