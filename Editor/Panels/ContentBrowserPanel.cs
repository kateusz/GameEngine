using System.Numerics;
using Editor.UI.Drawers;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels;

public class ContentBrowserPanel : IContentBrowserPanel
{
    private const float TreePanelWidth = 200f;

    private readonly ITextureFactory _textureFactory;
    private string _assetPath;
    private string _currentDirectory;
    private Texture2D _directoryIcon = null!;
    private Texture2D _fileIcon = null!;
    private readonly Dictionary<string, Texture2D> _imageCache = new();
    private readonly Dictionary<string, Texture2D> _folderIconCache = new();

    public ContentBrowserPanel(ITextureFactory textureFactory)
    {
        _textureFactory = textureFactory;
        _currentDirectory = Environment.CurrentDirectory;
        _assetPath = Path.Combine(_currentDirectory, "assets");
        _currentDirectory = _assetPath;
    }

    public void Init()
    {
        _directoryIcon = _textureFactory.Create("Resources/Icons/ContentBrowser/DirectoryIcon.png");
        _fileIcon = _textureFactory.Create("Resources/Icons/ContentBrowser/FileIcon.png");

        foreach (var name in new[] { "models", "animations", "scenes", "prefabs", "scripts", "sounds", "audio", "textures" })
        {
            var path = $"Resources/Icons/ContentBrowser/{name}.png";
            if (File.Exists(path))
                _folderIconCache[name] = _textureFactory.Create(path);
        }
    }

    public void Draw()
    {
        ImGui.Begin("Content Browser");

        ImGui.BeginChild("DirectoryTree", new Vector2(TreePanelWidth, 0), ImGuiChildFlags.Border);
        DrawDirectoryTree();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("ContentGrid", new Vector2(0, 0), ImGuiChildFlags.None);
        DrawContentGrid();
        ImGui.EndChild();

        ImGui.End();
    }

    private void DrawDirectoryTree()
    {
        DrawDirectoryNode(_assetPath);
    }

    private void DrawDirectoryNode(string directoryPath)
    {
        var dirName = Path.GetFileName(directoryPath) is { Length: > 0 } name ? name : "Assets";
        var isSelected = string.Equals(directoryPath, _currentDirectory, StringComparison.OrdinalIgnoreCase);

        string[] subdirectories;
        try
        {
            subdirectories = Directory.GetDirectories(directoryPath);
        }
        catch
        {
            subdirectories = [];
        }

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (subdirectories.Length == 0)
            flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        if (isSelected)
            flags |= ImGuiTreeNodeFlags.Selected;

        var isAncestor = _currentDirectory.StartsWith(directoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                         || isSelected;
        if (isAncestor && subdirectories.Length > 0)
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);

        var opened = ImGui.TreeNodeEx(dirName, flags);

        if (ImGui.IsItemClicked())
            _currentDirectory = directoryPath;

        if (opened && subdirectories.Length > 0)
        {
            foreach (var subdir in subdirectories)
                DrawDirectoryNode(subdir);
            ImGui.TreePop();
        }
    }

    private void DrawContentGrid()
    {
        ImGui.TextWrapped($"Current Path: {_currentDirectory}");
        ImGui.Separator();

        if (_currentDirectory != _assetPath)
        {
            ButtonDrawer.DrawCompactButton("<-", () =>
            {
                _currentDirectory = Directory.GetParent(_currentDirectory)!.FullName;
            });
        }

        var padding = 16.0f;
        var thumbnailSize = 36.0f;
        var cellSize = thumbnailSize + padding;

        var panelWidth = ImGui.GetContentRegionAvail().X;
        var columnCount = (int)(panelWidth / cellSize);
        if (columnCount < 1)
            columnCount = 1;

        ImGui.Columns(columnCount, "col", false);

        var entries = Directory.EnumerateFileSystemEntries(_currentDirectory);

        foreach (var entry in entries)
        {
            FileSystemInfo info = new FileInfo(entry);
            var relativePath = Path.GetRelativePath(_assetPath, entry);
            var isDirectory = (info.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
            var filenameString = info.Name;
            ImGui.PushID(filenameString);

            var (icon, isImage, isPrefab) = ResolveIcon(info, entry, isDirectory);
            var pointer = new IntPtr(icon.GetRendererId());

            ButtonDrawer.DrawTransparentIconButton(
                filenameString,
                icon.GetRendererId(),
                new Vector2(thumbnailSize, thumbnailSize));

            DragDropDrawer.CreateDragDropSource(
                "CONTENT_BROWSER_ITEM",
                relativePath,
                () => RenderDragDropPreview(filenameString, pointer, isImage, isPrefab, isDirectory));

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) &&
                !File.Exists(info.FullName))
            {
                _currentDirectory = info.FullName;
            }

            ImGui.TextWrapped(filenameString);
            ImGui.NextColumn();

            ImGui.PopID();
        }

        ImGui.Columns(1);
    }

    private (Texture2D icon, bool isImage, bool isPrefab) ResolveIcon(FileSystemInfo info, string entry, bool isDirectory)
    {
        if (isDirectory)
        {
            var folderName = info.Name.ToLowerInvariant();
            if (_folderIconCache.TryGetValue(folderName, out var folderIcon))
                return (folderIcon, false, false);
            return (_directoryIcon, false, false);
        }

        if (info.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            info.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            if (!_imageCache.TryGetValue(entry, out var cached))
            {
                try
                {
                    cached = _textureFactory.Create(entry);
                }
                catch
                {
                    cached = _fileIcon;
                }

                _imageCache[entry] = cached;
            }
            return (cached, true, false);
        }

        return (_fileIcon, false, false);
    }

    private static void RenderDragDropPreview(string filename, IntPtr pointer, bool isImage, bool isPrefab, bool isDirectory)
    {
        ImGui.Text($"Dragging: {filename}");
        if (isImage)
        {
            TextDrawer.DrawInfoText("Type: Texture");
            ImGui.Image(pointer, new Vector2(32, 32), new Vector2(0, 1), new Vector2(1, 0));
        }
        else if (isPrefab)
        {
            TextDrawer.DrawInfoText("Type: Prefab");
            ImGui.Image(pointer, new Vector2(32, 32), new Vector2(0, 1), new Vector2(1, 0));
        }
        else if (isDirectory)
            TextDrawer.DrawInfoText("Type: Directory");
        else
            TextDrawer.DrawInfoText($"Type: {Path.GetExtension(filename)}");
    }

    public void SetRootDirectory(string rootDir)
    {
        _assetPath = rootDir;
        _currentDirectory = rootDir;
    }
}