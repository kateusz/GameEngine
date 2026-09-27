using Editor.Platform;
using Editor.UI.Drawers;
using Engine.Project;

namespace Editor.AssetPicker;

public readonly record struct AssetFolderGroup(string Folder, IReadOnlyList<AssetEntry> Items);

/// <summary>
/// Non-UI state for Asset Browser: scan, filter, folder groups, path resolve, OS browse.
/// </summary>
public sealed class AssetPickSession(IProjectContext projectContext)
{
    private Action<string>? _onPicked;
    private string? _currentPathHint;
    private List<AssetEntry> _all = [];
    private List<AssetEntry> _filtered = [];
    private List<AssetFolderGroup> _groups = [];

    public bool IsOpen { get; private set; }
    public AssetKind? Kind { get; private set; }
    public string Search { get; private set; } = string.Empty;
    public bool Fuzzy { get; private set; } = true;
    public int SelectedIndex { get; set; } = -1;
    public string? StatusMessage { get; private set; }

    public IReadOnlyList<AssetEntry> Filtered => _filtered;
    public IReadOnlyList<AssetFolderGroup> Groups => _groups;
    public bool ShowsImageThumbnails => ReferenceEquals(Kind, AssetKind.Texture);
    public bool CanConfirm => SelectedIndex >= 0 && SelectedIndex < _filtered.Count;
    public bool HasProject => projectContext.Root is not null;

    public void Open(AssetKind kind, Action<string> onPicked, string? currentAssetRelativePath = null)
    {
        Kind = kind;
        _onPicked = onPicked;
        _currentPathHint = currentAssetRelativePath;
        Search = string.Empty;
        SelectedIndex = -1;
        StatusMessage = null;
        IsOpen = true;
        Rescan();
    }

    public void Close()
    {
        IsOpen = false;
        Kind = null;
        _onPicked = null;
        _currentPathHint = null;
        _all = [];
        _filtered = [];
        _groups = [];
        SelectedIndex = -1;
        StatusMessage = null;
        Search = string.Empty;
    }

    public void SetSearch(string search)
    {
        Search = search;
        RebuildFilter();
    }

    public void SetFuzzy(bool fuzzy)
    {
        if (Fuzzy == fuzzy)
            return;
        Fuzzy = fuzzy;
        RebuildFilter();
    }

    public bool TryConfirmSelected(out string relativePath)
    {
        relativePath = string.Empty;
        if (!CanConfirm)
            return false;
        return TryConfirm(_filtered[SelectedIndex], out relativePath);
    }

    public bool TryConfirm(AssetEntry entry, out string relativePath)
    {
        relativePath = string.Empty;
        if (_onPicked is null)
            return false;

        if (!TryToAssetRelative(entry.AbsolutePath, out relativePath))
        {
            StatusMessage = "Asset must live under the project assets folder.";
            return false;
        }

        var cb = _onPicked;
        Close();
        cb(relativePath);
        return true;
    }

    public bool TryBrowseOs()
    {
        if (Kind is null || _onPicked is null)
            return false;

        var relative = BrowseOs(Kind, _currentPathHint);
        if (string.IsNullOrEmpty(relative))
        {
            StatusMessage = "Could not use that file (wrong type or outside assets).";
            return false;
        }

        var cb = _onPicked;
        Close();
        cb(relative);
        return true;
    }

    public static string FolderOf(string displayPath)
    {
        var dir = Path.GetDirectoryName(displayPath)?.Replace('\\', '/');
        return string.IsNullOrEmpty(dir) ? "." : dir;
    }

    private void Rescan()
    {
        if (Kind is null || projectContext.Root is null)
        {
            _all = [];
            RebuildFilter();
            return;
        }

        _all = AssetScanner.Scan(projectContext.Root, Kind.Extensions);
        RebuildFilter();
    }

    private void RebuildFilter()
    {
        _filtered =
        [
            .. _all
                .Where(e => AssetScanner.MatchesSearch(e.DisplayPath, Search, Fuzzy))
        ];

        _groups =
        [
            .. _filtered
                .GroupBy(e => FolderOf(e.DisplayPath), StringComparer.OrdinalIgnoreCase)
                .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => new AssetFolderGroup(g.Key, [.. g]))
        ];

        if (SelectedIndex >= _filtered.Count)
            SelectedIndex = _filtered.Count > 0 ? 0 : -1;
    }

    private string? BrowseOs(AssetKind kind, string? currentAssetRelativePath)
    {
        if (!FilePicker.IsAvailable)
            return null;

        string? initial;
        try
        {
            initial = !string.IsNullOrEmpty(currentAssetRelativePath)
                ? PathBuilder.Resolve(currentAssetRelativePath)
                : PathBuilder.AssetsPath;
        }
        catch (InvalidOperationException)
        {
            initial = projectContext.Root ?? Environment.CurrentDirectory;
        }

        var picked = FilePicker.PickFile($"Open {kind.DisplayName}", kind.OsFileFilter, initial);
        if (string.IsNullOrEmpty(picked))
            return null;

        if (!DragDropDrawer.HasValidExtension(picked, kind.Extensions))
            return null;

        return TryToAssetRelative(picked, out var relative) ? relative : null;
    }

    private static bool TryToAssetRelative(string absolutePath, out string relative)
    {
        relative = string.Empty;
        try
        {
            relative = PathBuilder.ToAssetRelativePath(absolutePath);
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }
}
