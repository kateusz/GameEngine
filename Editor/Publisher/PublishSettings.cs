namespace Editor.Publisher;

public class PublishSettings
{
    public string OutputPath { get; set; } = string.Empty;
    public string RuntimeIdentifier { get; set; } = "win-x64";
    public bool SelfContained { get; set; } = true;
    public bool SingleFile { get; set; } = true;
    public string Configuration { get; set; } = "Release";
}
