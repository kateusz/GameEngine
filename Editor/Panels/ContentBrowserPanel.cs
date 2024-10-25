using System.Numerics;
using System.Runtime.InteropServices;
using Engine.Renderer.Textures;
using ImGuiNET;

namespace Editor.Panels;

public class ContentBrowserPanel
{
    private readonly string _assetPath;
    private string _currentDirectory;
    private readonly Texture2D _directoryIcon;
    private readonly Texture2D _fileIcon;

    public ContentBrowserPanel()
    {
        _currentDirectory = Environment.CurrentDirectory;
        _assetPath = Path.Combine(_currentDirectory, "assets");
        _currentDirectory = _assetPath;
        
        _directoryIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/DirectoryIcon.png");
        _fileIcon = TextureFactory.Create("Resources/Icons/ContentBrowser/FileIcon.png");
    }
    
    public void OnImGuiRender()
    {
        ImGui.Begin("Content Browser");

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
            
            var icon = isDirectory ? _directoryIcon : _fileIcon;
            var pointer  = new IntPtr(icon.GetRendererId());
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.ImageButton("", pointer, new Vector2(thumbnailSize, thumbnailSize), new Vector2(0, 1), new Vector2(1, 0));
            
            if (ImGui.BeginDragDropSource())
            {
                // Convert the relativePath string to a pointer (wide character format)
                IntPtr itemPathPtr = Marshal.StringToHGlobalUni(relativePath);  // Convert C# string to wchar_t* (IntPtr)

                // Calculate size in bytes, including the null terminator
                var itemPathSize = (relativePath.Length + 1) * sizeof(char);  // wchar_t is 2 bytes in C#
                
                ImGui.SetDragDropPayload("CONTENT_BROWSER_ITEM", itemPathPtr, (uint)itemPathSize);
                ImGui.EndDragDropSource();
            }
            
            ImGui.PopStyleColor();
            
            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
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
}