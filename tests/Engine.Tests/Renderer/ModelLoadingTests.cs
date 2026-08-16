using System.Numerics;
using Engine.Renderer;
using Engine.Renderer.Textures;
using NSubstitute;
using SceneComponents.Rendering;
using Shouldly;

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
