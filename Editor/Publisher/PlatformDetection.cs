using System.Runtime.InteropServices;

namespace Editor.Publisher;

public static class PlatformDetection
{
    public static string DetectCurrentPlatform()
    {
        if (OperatingSystem.IsWindows())
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "win-arm64" : "win-x64";

        if (OperatingSystem.IsMacOS())
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "osx-arm64" : "osx-x64";

        throw new PlatformNotSupportedException($"Unsupported platform: {RuntimeInformation.OSDescription}");
    }

    public static string GetPlatformDisplayName(string runtimeIdentifier)
    {
        return runtimeIdentifier switch
        {
            "win-x64" => "Windows (x64)",
            "win-x86" => "Windows (x86)",
            "win-arm64" => "Windows (ARM64)",
            "osx-x64" => "macOS (Intel)",
            "osx-arm64" => "macOS (Apple Silicon)",
            _ => runtimeIdentifier
        };
    }

    /// <summary>Apphost name produced by <c>dotnet publish</c> of Runtime.csproj.</summary>
    public static string GetExecutableName(string runtimeIdentifier)
        => runtimeIdentifier.StartsWith("win") ? "Runtime.exe" : "Runtime";

    /// <summary>Shipped player name derived from <paramref name="gameTitle"/>.</summary>
    public static string GetPublishedExecutableName(string runtimeIdentifier, string gameTitle)
    {
        var baseName = SanitizeExecutableBaseName(gameTitle);
        return runtimeIdentifier.StartsWith("win") ? $"{baseName}.exe" : baseName;
    }

    public static string SanitizeExecutableBaseName(string gameTitle)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(gameTitle.Where(c => Array.IndexOf(invalid, c) < 0).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Game" : cleaned;
    }
}
