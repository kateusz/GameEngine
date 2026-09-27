namespace Editor.Publisher;

public class PublishSettings
{
    public string OutputPath { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = "win-x64";
    public string Configuration { get; set; } = "Release";
}
