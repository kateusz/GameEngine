using System.Text.Json;
using System.Text.Json.Serialization;

namespace Editor.Publisher;

/// <summary>
/// Configuration settings for building and publishing games.
/// </summary>
public class BuildSettings
{
    /// <summary>
    /// Build configuration (Debug or Release)
    /// </summary>
    public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Release;

    /// <summary>
    /// Target platform for the build
    /// </summary>
    public TargetPlatform Platform { get; set; } = TargetPlatform.Win64;

    /// <summary>
    /// Startup scene path (relative to project root)
    /// </summary>
    public string StartupScene { get; set; } = "";

    /// <summary>
    /// Whether to pre-compile scripts into a DLL
    /// </summary>
    public bool PrecompileScripts { get; set; } = true;

    /// <summary>
    /// Include Roslyn for runtime script compilation (enables modding)
    /// </summary>
    public bool IncludeRoslyn { get; set; } = false;

    /// <summary>
    /// Asset packaging mode
    /// </summary>
    public AssetPackagingMode PackagingMode { get; set; } = AssetPackagingMode.LooseFiles;

    /// <summary>
    /// Optimize textures (compress, resize)
    /// </summary>
    public bool OptimizeTextures { get; set; } = false;

    /// <summary>
    /// Compress audio files to Ogg Vorbis
    /// </summary>
    public bool CompressAudio { get; set; } = false;

    /// <summary>
    /// Output directory for builds (absolute or relative to project)
    /// </summary>
    public string OutputDirectory { get; set; } = "Builds";

    /// <summary>
    /// Name of the game/executable
    /// </summary>
    public string GameName { get; set; } = "MyGame";

    /// <summary>
    /// Window title for the game
    /// </summary>
    public string WindowTitle { get; set; } = "Game";

    /// <summary>
    /// Default window width
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Default window height
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Start in fullscreen mode
    /// </summary>
    public bool Fullscreen { get; set; } = false;

    /// <summary>
    /// Enable VSync
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Loads build settings from a JSON file
    /// </summary>
    public static BuildSettings Load(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var settings = JsonSerializer.Deserialize<BuildSettings>(json);
                return settings ?? new BuildSettings();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading build settings: {ex.Message}");
        }

        return new BuildSettings();
    }

    /// <summary>
    /// Saves build settings to a JSON file
    /// </summary>
    public void Save(string filePath)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };

            var json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving build settings: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the runtime identifier string for dotnet publish
    /// </summary>
    public string GetRuntimeIdentifier()
    {
        return Platform switch
        {
            TargetPlatform.Win64 => "win-x64",
            TargetPlatform.Win86 => "win-x86",
            TargetPlatform.WinARM64 => "win-arm64",
            TargetPlatform.Linux64 => "linux-x64",
            TargetPlatform.LinuxARM64 => "linux-arm64",
            TargetPlatform.MacOS64 => "osx-x64",
            TargetPlatform.MacOSARM64 => "osx-arm64",
            _ => "win-x64"
        };
    }

    /// <summary>
    /// Gets the executable extension for the target platform
    /// </summary>
    public string GetExecutableExtension()
    {
        return Platform switch
        {
            TargetPlatform.Win64 => ".exe",
            TargetPlatform.Win86 => ".exe",
            TargetPlatform.WinARM64 => ".exe",
            TargetPlatform.Linux64 => "",
            TargetPlatform.LinuxARM64 => "",
            TargetPlatform.MacOS64 => "",
            TargetPlatform.MacOSARM64 => "",
            _ => ".exe"
        };
    }
}

/// <summary>
/// Build configuration type
/// </summary>
public enum BuildConfiguration
{
    Debug,
    Release
}

/// <summary>
/// Target platform for builds
/// </summary>
public enum TargetPlatform
{
    Win64,          // Windows x64
    Win86,          // Windows x86 (legacy)
    WinARM64,       // Windows ARM64 (Surface devices)
    Linux64,        // Linux x64
    LinuxARM64,     // Linux ARM64 (Raspberry Pi, etc.)
    MacOS64,        // macOS Intel
    MacOSARM64      // macOS Apple Silicon (M1/M2/M3)
}

/// <summary>
/// Asset packaging mode for builds
/// </summary>
public enum AssetPackagingMode
{
    LooseFiles,     // Keep assets as individual files (easy modding)
    PackedArchive,  // Pack assets into .pak file (Phase 3)
    Embedded        // Embed assets in executable (Phase 3)
}
