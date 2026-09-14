using Editor.Publisher;
using Shouldly;

namespace Editor.Tests.Publisher;

public class PlatformDetectionTests
{
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
