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

        if (OperatingSystem.IsLinux())
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                ? "linux-arm64" : "linux-x64";

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
            "linux-x64" => "Linux (x64)",
            "linux-arm64" => "Linux (ARM64)",
            _ => runtimeIdentifier
        };
    }

    public static string GetExecutableName(string runtimeIdentifier)
    {
        return runtimeIdentifier.StartsWith("win") ? "Runtime.exe" : "Runtime";
    }
}
