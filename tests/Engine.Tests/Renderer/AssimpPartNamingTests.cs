using Engine.Renderer;
using Shouldly;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class AssimpPartNamingTests
{
    [Theory]
    [InlineData("Cube.088", "Cube.088")]
    [InlineData("a/b\\c", "a_b_c")]
    [InlineData("  ", "part")]
    [InlineData("..", "part")]
    public void Sanitize_ReplacesInvalidOrEmpty(string raw, string expected) =>
        AssimpPartNaming.Sanitize(raw).ShouldBe(expected);

    [Fact]
    public void UniqueSanitize_AppendsSuffixOnDuplicates()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        AssimpPartNaming.UniqueSanitize("Door", counts).ShouldBe("Door");
        AssimpPartNaming.UniqueSanitize("Door", counts).ShouldBe("Door_1");
        AssimpPartNaming.UniqueSanitize("Door", counts).ShouldBe("Door_2");
        AssimpPartNaming.UniqueSanitize("Roof", counts).ShouldBe("Roof");
    }
}
