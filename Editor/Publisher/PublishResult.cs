namespace Editor.Publisher;

/// <summary>
/// Result of a game publishing operation.
/// </summary>
public class PublishResult
{
    /// <summary>
    /// Whether the publish operation succeeded.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// Path to the published output directory (when successful).
    /// </summary>
    public string? OutputPath { get; init; }
    
    /// <summary>
    /// Error message describing the failure (when unsuccessful).
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Build output messages from the dotnet publish process.
    /// </summary>
    public List<string> BuildOutput { get; init; } = new();

    /// <summary>
    /// Creates a successful publish result.
    /// </summary>
    public static PublishResult Succeeded(string outputPath) => 
        new() { Success = true, OutputPath = outputPath };
    
    /// <summary>
    /// Creates a failed publish result with the given error message.
    /// </summary>
    public static PublishResult Failed(string error) => 
        new() { Success = false, ErrorMessage = error };
}
