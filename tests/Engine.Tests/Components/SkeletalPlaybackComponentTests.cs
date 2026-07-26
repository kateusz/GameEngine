using System.Numerics;
using System.Text.Json;
using SceneComponents.Rendering;
using Shouldly;

namespace Engine.Tests.Components;

[Trait("Category", "Unit")]
public class SkeletalPlaybackComponentTests
{
    [Fact]
    public void Defaults_Clone_AndSerialize_PathsOnly_BonePaletteIgnored()
    {
        var component = new SkeletalPlaybackComponent();

        component.SkeletonPath.ShouldBeNull();
        component.ClipPath.ShouldBeNull();
        component.ClipName.ShouldBeNull();
        component.Time.ShouldBe(0f);
        component.Speed.ShouldBe(1f);
        component.Loop.ShouldBeTrue();
        component.Playing.ShouldBeFalse();
        component.BonePalette.Length.ShouldBe(100);

        component.SkeletonPath = "models/hero.skel";
        component.ClipPath = "models/hero.anim3d";
        component.ClipName = "Walk";
        component.Time = 1.25f;
        component.Speed = 2f;
        component.Loop = false;
        component.Playing = true;
        component.BonePalette[0] = Matrix4x4.CreateTranslation(9, 0, 0);

        var clone = (SkeletalPlaybackComponent)component.Clone();
        clone.ShouldNotBeSameAs(component);
        clone.SkeletonPath.ShouldBe("models/hero.skel");
        clone.ClipPath.ShouldBe("models/hero.anim3d");
        clone.ClipName.ShouldBe("Walk");
        clone.Time.ShouldBe(1.25f);
        clone.Speed.ShouldBe(2f);
        clone.Loop.ShouldBeFalse();
        clone.Playing.ShouldBeTrue();
        clone.BonePalette.Length.ShouldBe(100);
        clone.BonePalette[0].ShouldBe(Matrix4x4.Identity);

        var json = JsonSerializer.Serialize(component);
        json.ShouldContain("SkeletonPath");
        json.ShouldContain("ClipPath");
        json.ShouldContain("ClipName");
        json.ShouldNotContain("BonePalette");
    }
}
