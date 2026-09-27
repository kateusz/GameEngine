namespace Editor.UI;

// ponytail: ImGui is single-threaded; set by PropertiesPanel around DrawAllComponents
public static class PropertySearch
{
    public static string? Filter;

    public static bool Matches(string label) =>
        string.IsNullOrWhiteSpace(Filter)
        || label.Contains(Filter.Trim(), StringComparison.OrdinalIgnoreCase);
}
