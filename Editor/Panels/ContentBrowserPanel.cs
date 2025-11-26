using System.Numerics;
using Editor.UI.Drawers;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels;

public class ContentBrowserPanel : IContentBrowserPanel
{
    private string _assetPath;
    private string _currentDirectory;
    private Texture2D _directoryIcon = null!;
    private Texture2D _fileIcon = null!;
    private Texture2D _prefabIcon = null!;
    private readonly Dictionary<string, Texture2D> _imageCache = new();

    public ContentBrowserPanel()
    {
        _currentDirectory = Environment.CurrentDirectory;
        _assetPath = Path.Combine(_currentDirectory, "assets");
        _currentDirectory = _assetPath;
    }

    public void Init()
    {
        _directoryIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/DirectoryIcon.png");
        _fileIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/FileIcon.png");
        _prefabIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/PrefabIcon.png");
    }

    public void Draw()
    {
        ImGui.Begin("Content Browser");

        // Display current path at the top
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

            Texture2D icon;
            bool isImage = false;
            bool isPrefab = false;

            if (isDirectory)
            {
                icon = _directoryIcon;
            }
            else if (info.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                     info.Name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                isImage = true;
                if (!_imageCache.TryGetValue(entry, out icon!))
                {
                    try
                    {
                        icon = TextureFactory.Create(entry);
                        _imageCache[entry] = icon;
                    }
                    catch
                    {
                        icon = _fileIcon;
                    }
                }
            }
            else if (info.Name.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                isPrefab = true;
                icon = _prefabIcon;
            }
            else if (info.Name.EndsWith(".obj", StringComparison.OrdinalIgnoreCase) ||
                     info.Name.EndsWith(".scene", StringComparison.OrdinalIgnoreCase))
            {
                icon = _fileIcon;
            }
            else if (info.Name.EndsWith(".anim", StringComparison.OrdinalIgnoreCase))
            {
                // TODO: animation icon
                icon = _fileIcon;
            }
            else
            {
                icon = _fileIcon;
            }

            var pointer = new IntPtr(icon.GetRendererId());

            // Draw transparent icon button
            ButtonDrawer.DrawTransparentIconButton(
                filenameString,
                icon.GetRendererId(),
                new Vector2(thumbnailSize, thumbnailSize));

            // Setup drag and drop source
            DragDropDrawer.CreateDragDropSource(
                "CONTENT_BROWSER_ITEM",
                relativePath,
                () =>
                {
                    ImGui.Text($"Dragging: {filenameString}");
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
                    {
                        TextDrawer.DrawInfoText("Type: Directory");
                    }
                    else
                    {
                        TextDrawer.DrawInfoText($"Type: {Path.GetExtension(filenameString)}");
                    }
                });

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
        ImGui.End();
    }

    public void SetRootDirectory(string rootDir)
    {
        _assetPath = rootDir;
        _currentDirectory = rootDir;
    }
}