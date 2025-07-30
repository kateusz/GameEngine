using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels;

public class ContentBrowserPanel
{
    private string _assetPath;
    private string _currentDirectory;
    private readonly Texture2D _directoryIcon;
    private readonly Texture2D _fileIcon;
    private readonly Texture2D _prefabIcon;
    private readonly Dictionary<string, Texture2D> _imageCache = new();

    public ContentBrowserPanel()
    {
        _currentDirectory = Environment.CurrentDirectory;
        _assetPath = Path.Combine(_currentDirectory, "assets");
        _currentDirectory = _assetPath;

        _directoryIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/DirectoryIcon.png");
        _fileIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/FileIcon.png");
        _prefabIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/PrefabIcon.png");
    }

    public void OnImGuiRender()
    {
        ImGui.Begin("Content Browser");

        // Display current path at the top
        ImGui.TextWrapped($"Current Path: {_currentDirectory}");
        ImGui.Separator();

        if (_currentDirectory != _assetPath)
        {
            if (ImGui.Button("<-"))
            {
                _currentDirectory = Directory.GetParent(_currentDirectory)!.FullName;
            }
        }

        var padding = 16.0f;
        var thumbnailSize = 64.0f;
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
                if (!_imageCache.TryGetValue(entry, out icon))
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
            else
            {
                icon = _fileIcon;
            }

            var pointer = new IntPtr(icon.GetRendererId());
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.ImageButton("", pointer, new Vector2(thumbnailSize, thumbnailSize),
                new Vector2(0, 1), new Vector2(1, 0));

            if (ImGui.BeginDragDropSource())
            {
                IntPtr itemPathPtr = Marshal.StringToHGlobalUni(relativePath);
                var itemPathSize = (relativePath.Length + 1) * sizeof(char);
                ImGui.SetDragDropPayload("CONTENT_BROWSER_ITEM", itemPathPtr, (uint)itemPathSize);

                // Show preview of what we're dragging
                ImGui.Text($"Dragging: {filenameString}");
                if (isImage)
                {
                    ImGui.Text("Type: Texture");
                    // Show small icon
                    ImGui.Image(pointer, new Vector2(32, 32), new Vector2(0, 1), new Vector2(1, 0));
                }
                else if (isPrefab)
                {
                    ImGui.Text("Type: Prefab");
                    ImGui.Image(pointer, new Vector2(32, 32), new Vector2(0, 1), new Vector2(1, 0));
                }
                else if (isDirectory)
                {
                    ImGui.Text("Type: Directory");
                }
                else
                {
                    ImGui.Text($"Type: {Path.GetExtension(filenameString)}");
                }

                ImGui.EndDragDropSource();
            }

            ImGui.PopStyleColor();

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