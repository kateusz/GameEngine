namespace Editor.AssetPicker;

public sealed class AssetKind
{
    public required string DisplayName { get; init; }
    public required string[] Extensions { get; init; }
    public required string OsFileFilter { get; init; }

    public static AssetKind Texture { get; } = new()
    {
        DisplayName = "Texture",
        Extensions = [".png", ".jpg"],
        OsFileFilter = BuildOsFilter("Images", [".png", ".jpg"]),
    };

    public static AssetKind Model { get; } = new()
    {
        DisplayName = "Model",
        Extensions = [".glb", ".gltf", ".fbx"],
        OsFileFilter = BuildOsFilter("3D Models", [".glb", ".gltf", ".fbx"]),
    };

    public static AssetKind Audio { get; } = new()
    {
        DisplayName = "Audio",
        Extensions = [".wav", ".ogg"],
        OsFileFilter = BuildOsFilter("Audio", [".wav", ".ogg"]),
    };

    /// <summary>
    /// Builds a WinForms-style filter string from a label and extensions — single source of truth.
    /// </summary>
    public static string BuildOsFilter(string label, string[] extensions)
    {
        var pattern = string.Join(";", extensions.Select(e => "*" + e));
        return $"{label} ({pattern})|{pattern}|All files (*.*)|*.*";
    }
}
