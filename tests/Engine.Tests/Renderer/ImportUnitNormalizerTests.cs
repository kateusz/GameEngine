using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Skeletal;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class ImportUnitNormalizerTests
{
    [Fact]
    public void DetectDownscaleFactor_OversizedMesh_ReturnsCmToMeters()
    {
        var mesh = new Mesh("big");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(0f, 180f, 0f) });
        mesh.Indices.Add(0);

        var factor = ImportUnitNormalizer.DetectDownscaleFactor([new ModelSubmesh(mesh, new MeshMaterial())]);

        factor.ShouldBe(ImportUnitNormalizer.CmToMeters);
    }

    [Fact]
    public void DetectDownscaleFactor_MeterSizedMesh_ReturnsOne()
    {
        var mesh = new Mesh("small");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(0f, 1.8f, 0f) });
        mesh.Indices.Add(0);

        var factor = ImportUnitNormalizer.DetectDownscaleFactor([new ModelSubmesh(mesh, new MeshMaterial())]);

        factor.ShouldBe(1f);
    }

    [Fact]
    public void HarmonizeInverseBindWithMesh_DownscalesCmOffsets()
    {
        var mesh = new Mesh("human");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new Vector3(0f, 1.8f, 0f) });
        mesh.Indices.Add(0);
        var ib = Matrix4x4.CreateTranslation(0f, 180f, 0f);
        var skeleton = new SkeletonAsset([new SkeletonBone("hips", -1, ib)]);

        var fixedSkel = ImportUnitNormalizer.HarmonizeInverseBindWithMesh(
            skeleton, [new ModelSubmesh(mesh, new MeshMaterial())]);

        var t = new Vector3(
            fixedSkel.Bones[0].InverseBind.M41,
            fixedSkel.Bones[0].InverseBind.M42,
            fixedSkel.Bones[0].InverseBind.M43);
        t.Y.ShouldBe(1.8f, 0.01f);
    }

    [Fact]
    public void DetectScaleFactors_MeterMeshWithCmAnimationKeys_ScalesAnimOnly()
    {
        var mesh = new Mesh("human");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(0f, 1.8f, 0f) });
        mesh.Indices.Add(0);

        var anim = new Anim3dAsset(
        [
            new Anim3dClip(
                "mixamo",
                1f,
                [
                    new Anim3dChannel(
                        0,
                        [new Anim3dVec3Key(0f, new System.Numerics.Vector3(0f, 165f, 0f))],
                        [],
                        [])
                ])
        ]);

        var scale = ImportUnitNormalizer.DetectScaleFactors(
            [new ModelSubmesh(mesh, new MeshMaterial())], anim);

        scale.Mesh.ShouldBe(1f);
        scale.Anim.ShouldBe(ImportUnitNormalizer.CmToMeters);
    }

    [Fact]
    public void ApplyToSubmeshes_ScalesVertexPositions()
    {
        var mesh = new Mesh("big");
        mesh.Vertices.Add(new Mesh.Vertex { Position = new System.Numerics.Vector3(0f, 200f, 0f) });
        mesh.Indices.Add(0);
        var submeshes = new[] { new ModelSubmesh(mesh, new MeshMaterial()) };

        ImportUnitNormalizer.ApplyToSubmeshes(submeshes, ImportUnitNormalizer.CmToMeters);

        mesh.Vertices[0].Position.Y.ShouldBe(2f, 0.001f);
    }
}
