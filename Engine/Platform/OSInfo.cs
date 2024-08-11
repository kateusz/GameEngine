using System.Runtime.InteropServices;

namespace Engine.Platform;

public static class OSInfo
{
    // Property to check if the operating system is Windows
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // Property to check if the operating system is macOS
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    // Property to check if the operating system is Linux
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    // Property to get a string description of the operating system
    public static string OSDescription => RuntimeInformation.OSDescription;

    // Property to get the architecture of the operating system
    public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;
}