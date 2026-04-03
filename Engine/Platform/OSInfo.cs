using System.Runtime.InteropServices;

namespace Engine.Platform;

internal static class OSInfo
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static string OSDescription => RuntimeInformation.OSDescription;
    public static Architecture OSArchitecture => RuntimeInformation.OSArchitecture;
}