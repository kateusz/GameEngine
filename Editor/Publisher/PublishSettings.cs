namespace Editor.Publisher;

/// <summary>
/// Settings for publishing/building a game project.
/// </summary>
public class PublishSettings
{
    /// <summary>
    /// Output directory path for the published build.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target runtime identifier (e.g., win-x64, osx-x64, linux-x64, osx-arm64, linux-arm64).
    /// </summary>
    public string RuntimeIdentifier { get; set; } = "win-x64";
    
    /// <summary>
    /// Whether to include the .NET runtime in the build output.
    /// </summary>
    public bool SelfContained { get; set; } = true;
    
    /// <summary>
    /// Whether to publish as a single executable file.
    /// </summary>
    public bool SingleFile { get; set; } = true;
    
    /// <summary>
    /// Whether to create a distributable package (e.g., zip archive).
    /// </summary>
    public bool CreatePackage { get; set; } = false;
    
    /// <summary>
    /// Build configuration (Release or Debug).
    /// </summary>
    public string Configuration { get; set; } = "Release";
}
