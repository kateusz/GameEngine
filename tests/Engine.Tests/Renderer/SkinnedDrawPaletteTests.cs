using System.Numerics;
using ECS;
using Engine.Renderer;
using Engine.Renderer.Skeletal;
using Engine.Scene;
using Engine.Scene.Skeletal;
using Engine.Scene.Cameras;
using NSubstitute;
using SceneComponents;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class SkinnedDrawPaletteTests
{
    [Fact]
    public void Evaluate_AtBindKeys_PaletteIsIdentity_AndPadsUnusedSlots()
    {
        var skeleton = new SkeletonAsset(
        [
            new SkeletonBone("only", -1, Matrix4x4.Identity)
        ]);
        var clip = new Anim3dClip(
            "Pose",
            1f,
            [
                new Anim3dChannel(
                    0,
                    [
                        new Anim3dVec3Key(0f, Vector3.Zero),
                        new Anim3dVec3Key(1f, new Vector3(5, 0, 0))
                    ],
                    [new Anim3dQuatKey(0f, Quaternion.Identity), new Anim3dQuatKey(1f, Quaternion.Identity)],
                    [new Anim3dVec3Key(0f, Vector3.One), new Anim3dVec3Key(1f, Vector3.One)])
            ]);

        var palette = new Matrix4x4[SkeletalPoseMath.MaxBones];
        Array.Fill(palette, Matrix4x4.CreateScale(99f));
        SkeletalPoseMath.Evaluate(skeleton, clip, 0f, palette);

        palette[0].ShouldBe(Matrix4x4.Identity);
        for (var i = 1; i < 100; i++)
            palette[i].ShouldBe(Matrix4x4.Identity);

        SkeletalPoseMath.Evaluate(skeleton, clip, 0.5f, palette);
        palette[0].ShouldNotBe(Matrix4x4.Identity);

        var context = new Context();
        var graphics3D = Substitute.For<IGraphics3D>();
        var modelFactory = Substitute.For<IModelFactory>();
        var mesh = new Mesh("stub");
        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
        modelFactory.Create(Arg.Any<string>()).Returns(model);

        var parent = Entity.Create(1, "parent");
        var playback = new SkeletalPlaybackComponent
        {
            Playing = false,
            BonePalette = palette
        };
        parent.AddComponent(playback);
        parent.AddComponent(new TransformComponent());
        context.Register(parent);

        var child = Entity.Create(2, "child");
        child.AddComponent(new ModelRendererComponent { ModelPath = "/tmp/shared.mesh" });
        child.AddComponent(new TransformComponent());
        child.AddComponent(new ParentComponent(parent.Id));
        context.Register(child);

        var lone = Entity.Create(3, "lone");
        lone.AddComponent(new ModelRendererComponent { ModelPath = "/tmp/shared.mesh" });
        lone.AddComponent(new TransformComponent());
        context.Register(lone);

        var camera = new SceneRenderPipeline.CameraBinding
        {
            ViewCamera = Substitute.For<IViewCamera>()
        };

        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics3D,
            null,
            modelFactory,
            camera);

        graphics3D.Received().DrawMesh(
            Arg.Any<Matrix4x4>(),
            Arg.Any<Mesh>(),
            Arg.Any<MeshMaterial>(),
            Arg.Any<Vector4>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            child.Id,
            Arg.Is<Matrix4x4[]>(p => p.Length == 100 && p.All(m => m == Matrix4x4.Identity)));

        graphics3D.Received().DrawMesh(
            Arg.Any<Matrix4x4>(),
            Arg.Any<Mesh>(),
            Arg.Any<MeshMaterial>(),
            Arg.Any<Vector4>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            lone.Id,
            Arg.Is<Matrix4x4[]>(p => p.Length == 100 && p.All(m => m == Matrix4x4.Identity)));
    }

    [Fact]
    public void RenderModels_SelfPlaybackPalette_WhenPlaybackOnSameEntity()
    {
        var context = new Context();
        var graphics3D = Substitute.For<IGraphics3D>();
        var modelFactory = Substitute.For<IModelFactory>();
        var mesh = new Mesh("stub");
        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
        modelFactory.Create(Arg.Any<string>()).Returns(model);

        var expected = Matrix4x4.CreateTranslation(3, 0, 0);
        var entity = Entity.Create(20, "self-skinned");
        var playback = new SkeletalPlaybackComponent { Playing = true };
        playback.BonePalette[0] = expected;
        entity.AddComponent(playback);
        entity.AddComponent(new ModelRendererComponent { ModelPath = "/tmp/char.mesh" });
        entity.AddComponent(new TransformComponent());
        context.Register(entity);

        var camera = new SceneRenderPipeline.CameraBinding
        {
            ViewCamera = Substitute.For<IViewCamera>()
        };

        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics3D,
            null,
            modelFactory,
            camera);

        graphics3D.Received(1).DrawMesh(
            Arg.Any<Matrix4x4>(),
            Arg.Any<Mesh>(),
            Arg.Any<MeshMaterial>(),
            Arg.Any<Vector4>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            entity.Id,
            Arg.Is<Matrix4x4[]>(p => p[0] == expected));
    }

    [Fact]
    public void RenderModels_ResolvesParentPlaybackPaletteForChildModelRenderer()
    {
        var context = new Context();
        var graphics3D = Substitute.For<IGraphics3D>();
        var modelFactory = Substitute.For<IModelFactory>();
        var mesh = new Mesh("stub");
        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
        modelFactory.Create(Arg.Any<string>()).Returns(model);

        var expected = Matrix4x4.CreateTranslation(7, 0, 0);
        var parent = Entity.Create(10, "rig");
        var playback = new SkeletalPlaybackComponent { Playing = true };
        playback.BonePalette[0] = expected;
        parent.AddComponent(playback);
        parent.AddComponent(new TransformComponent());
        context.Register(parent);

        var child = Entity.Create(11, "mesh");
        child.AddComponent(new ModelRendererComponent { ModelPath = "/tmp/char.mesh" });
        child.AddComponent(new TransformComponent());
        child.AddComponent(new ParentComponent(parent.Id));
        context.Register(child);

        var camera = new SceneRenderPipeline.CameraBinding
        {
            ViewCamera = Substitute.For<IViewCamera>()
        };

        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics3D,
            null,
            modelFactory,
            camera);

        graphics3D.Received(1).DrawMesh(
            Arg.Any<Matrix4x4>(),
            Arg.Any<Mesh>(),
            Arg.Any<MeshMaterial>(),
            Arg.Any<Vector4>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            child.Id,
            Arg.Is<Matrix4x4[]>(p => p[0] == expected));
    }

    [Fact]
    public void RenderModels_ResolvesGrandparentPlaybackPalette()
    {
        var context = new Context();
        var graphics3D = Substitute.For<IGraphics3D>();
        var modelFactory = Substitute.For<IModelFactory>();
        var mesh = new Mesh("stub");
        var model = new Model([new ModelSubmesh(mesh, new MeshMaterial())]);
        modelFactory.Create(Arg.Any<string>()).Returns(model);

        var expected = Matrix4x4.CreateTranslation(9, 0, 0);
        var root = Entity.Create(30, "root");
        var playback = new SkeletalPlaybackComponent { Playing = true };
        playback.BonePalette[0] = expected;
        root.AddComponent(playback);
        root.AddComponent(new TransformComponent());
        context.Register(root);

        var mid = Entity.Create(31, "mid");
        mid.AddComponent(new TransformComponent());
        mid.AddComponent(new ParentComponent(root.Id));
        context.Register(mid);

        var leaf = Entity.Create(32, "leaf");
        leaf.AddComponent(new ModelRendererComponent { ModelPath = "/tmp/char.mesh" });
        leaf.AddComponent(new TransformComponent());
        leaf.AddComponent(new ParentComponent(mid.Id));
        context.Register(leaf);

        var camera = new SceneRenderPipeline.CameraBinding
        {
            ViewCamera = Substitute.For<IViewCamera>()
        };

        SceneRenderPipeline.RenderScene(
            context,
            Substitute.For<IGraphics2D>(),
            graphics3D,
            null,
            modelFactory,
            camera);

        graphics3D.Received(1).DrawMesh(
            Arg.Any<Matrix4x4>(),
            Arg.Any<Mesh>(),
            Arg.Any<MeshMaterial>(),
            Arg.Any<Vector4>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            leaf.Id,
            Arg.Is<Matrix4x4[]>(p => p[0] == expected));
    }
}
