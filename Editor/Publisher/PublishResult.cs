namespace Editor.Publisher;

public class PublishResult
{
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> BuildOutput { get; init; } = [];

    public static PublishResult Ok(List<string>? buildOutput = null) =>
        new() { Success = true, BuildOutput = buildOutput ?? [] };

    public static PublishResult Succeeded(string outputPath, List<string>? buildOutput = null) =>
        new() { Success = true, OutputPath = outputPath, BuildOutput = buildOutput ?? [] };

    public static PublishResult Failed(string error, List<string>? buildOutput = null) =>
        new() { Success = false, ErrorMessage = error, BuildOutput = buildOutput ?? [] };
}
