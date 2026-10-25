using Editor.Publisher;
using Shouldly;

namespace Editor.Tests.Publisher;

public class PlatformDetectionTests
{
    [Fact]
    public void SupportedRuntimeIdentifiers_lists_all_publish_targets()
    {
        PlatformDetection.SupportedRuntimeIdentifiers.ShouldBe(
        [
            "win-x64", "win-x86", "win-arm64",
            "osx-x64", "osx-arm64",
            "linux-x64", "linux-arm64",
        ]);
    }

    [Fact]
    public void GetPublishedExecutableName_win_x64_appends_exe()
    {
        PlatformDetection.GetPublishedExecutableName("win-x64", "Snake").ShouldBe("Snake.exe");
    }

    [Fact]
    public void GetPublishedExecutableName_osx_keeps_spaces_no_extension()
    {
        PlatformDetection.GetPublishedExecutableName("osx-arm64", "Flappy Bird").ShouldBe("Flappy Bird");
    }

    [Fact]
    public void GetPublishedExecutableName_linux_has_no_exe_extension()
    {
        PlatformDetection.GetPublishedExecutableName("linux-x64", "Snake").ShouldBe("Snake");
    }

    [Theory]
    [InlineData("linux-x64", "Linux (x64)")]
    [InlineData("linux-arm64", "Linux (ARM64)")]
    public void GetPlatformDisplayName_linux_targets(string rid, string expected)
    {
        PlatformDetection.GetPlatformDisplayName(rid).ShouldBe(expected);
    }

    [Fact]
    public void GetPublishedExecutableName_strips_invalid_file_chars()
    {
        var dirty = "Game" + new string(Path.GetInvalidFileNameChars());
        PlatformDetection.GetPublishedExecutableName("win-x64", dirty).ShouldBe("Game.exe");
    }

    [Fact]
    public void GetPublishedExecutableName_empty_title_falls_back_to_Game()
    {
        PlatformDetection.GetPublishedExecutableName("win-x64", "   ").ShouldBe("Game.exe");
    }

    [Fact]
    public void GetExecutableName_is_dotnet_publish_apphost()
    {
        PlatformDetection.GetExecutableName("win-x64").ShouldBe("Runtime.exe");
        PlatformDetection.GetExecutableName("osx-arm64").ShouldBe("Runtime");
    }
}
