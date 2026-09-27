using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Editor.AssetPicker;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Project;
using Engine.Renderer.Textures;
using Engine.Scripting;
using ImGuiNET;
using Serilog;
using Ui.ImGui;

namespace Editor.Panels;

public class ContentBrowserPanel : IDisposable
{
    private enum CreateAssetKind { Component, System }

    private const float TreePanelWidth = 200f;
    private static readonly Regex ValidNameRegex = new(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
    private static readonly ILogger Logger = Log.ForContext<ContentBrowserPanel>();

    private readonly ITextureFactory _textureFactory;
    private readonly AssetThumbnailCache _thumbnails;
    private readonly IProjectContext _projectContext;
    private readonly ContentBrowserActions _actions;
    private string _assetPath;
    private string _currentDirectory;
    private Texture2D _directoryIcon = null!;
    private Texture2D _fileIcon = null!;
    private readonly Dictionary<string, Texture2D> _imageCache = new();
    private readonly Dictionary<string, Texture2D> _folderIconCache = new();
    private bool _disposed;

    private const string CreateAssetPopupId = "ContentBrowserCreateAsset";

    private CreateAssetKind _pendingCreateKind;
    private bool _showNameModal;
    private bool _queueCreateAssetModal;
    private string _createAssetPopupName = string.Empty;
    private string _newAssetName = string.Empty;
    private string? _errorMessage;
    private string _folderFilter = string.Empty;
    private bool _pendingTreeExpand = true; // open path to current dir on first draw

    public ContentBrowserPanel(
        ITextureFactory textureFactory,
        AssetThumbnailCache thumbnails,
        IProjectContext projectContext,
        ContentBrowserActions actions)
    {
        _textureFactory = textureFactory;
        _thumbnails = thumbnails;
        _projectContext = projectContext;
        _actions = actions;
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

    public void DrawContent()
    {
        ImGui.BeginChild("DirectoryTree", new Vector2(TreePanelWidth, 0), ImGuiChildFlags.Border);
        DrawDirectoryTree();
        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild("ContentGrid", new Vector2(0, 0), ImGuiChildFlags.None);
        DrawContentGrid();
        ImGui.EndChild();
    }

    public void RenderPopups() => RenderCreateAssetModal();

    private void DrawDirectoryTree()
    {
        DrawDirectoryNode(_assetPath);
        _pendingTreeExpand = false;
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

        // Only force-open the path to the current folder when navigating — Always every
        // frame made collapse impossible.
        var isAncestor = _currentDirectory.StartsWith(directoryPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                         || isSelected;
        if (_pendingTreeExpand && isAncestor && subdirectories.Length > 0)
            ImGui.SetNextItemOpen(true, ImGuiCond.Always);
        
        var opened = ImGui.TreeNodeEx($"{dirName}##{directoryPath}", flags);

        if (ImGui.IsItemClicked())
            NavigateTo(directoryPath);

        if (ImGui.BeginPopupContextItem($"DirCtx##{directoryPath}"))
        {
            if (OperatingSystem.IsWindows())
            {
                if (ImGui.MenuItem("Show in Explorer"))
                    ShowInExplorer(directoryPath);
                ImGui.Separator();
            }

            var canCreate = CanCreateScriptAssets(directoryPath);
            if (ImGui.MenuItem("Add Component", enabled: canCreate))
                BeginCreateAsset(CreateAssetKind.Component);
            if (ImGui.MenuItem("Add System", enabled: canCreate))
                BeginCreateAsset(CreateAssetKind.System);
            ImGui.EndPopup();
        }

        if (opened && subdirectories.Length > 0)
        {
            foreach (var subdir in subdirectories)
                DrawDirectoryNode(subdir);
            ImGui.TreePop();
        }
    }

    private bool CanCreateScriptAssets(string directoryPath)
    {
        if (_projectContext.ScriptsDir is not { } scriptsDir)
            return false;

        var fullDir = Path.GetFullPath(directoryPath);
        var fullScripts = Path.GetFullPath(scriptsDir);
        return fullDir.Equals(fullScripts, StringComparison.OrdinalIgnoreCase)
               || fullDir.StartsWith(fullScripts + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private void BeginCreateAsset(CreateAssetKind kind)
    {
        _pendingCreateKind = kind;
        _errorMessage = null;
        _newAssetName = kind switch
        {
            CreateAssetKind.Component => string.Empty,
            CreateAssetKind.System => "MyGame",
            _ => string.Empty
        };
        _createAssetPopupName = kind switch
        {
            CreateAssetKind.Component => $"Create Game Component##{CreateAssetPopupId}",
            CreateAssetKind.System => $"Create Game System##{CreateAssetPopupId}",
            _ => $"Create Asset##{CreateAssetPopupId}"
        };
        _queueCreateAssetModal = true;
    }

    private void RenderCreateAssetModal()
    {
        if (_queueCreateAssetModal)
        {
            _queueCreateAssetModal = false;
            _showNameModal = true;
            ImGui.OpenPopup(_createAssetPopupName);
            return;
        }

        if (!_showNameModal)
            return;

        if (!ImGui.IsPopupOpen(_createAssetPopupName, ImGuiPopupFlags.AnyPopupId))
        {
            _showNameModal = false;
            return;
        }

        var isValidName = !string.IsNullOrEmpty(_newAssetName) && ValidNameRegex.IsMatch(_newAssetName);
        var promptText = _pendingCreateKind switch
        {
            CreateAssetKind.Component => isValidName
                ? $"Enter base name for the new component:\nWill create: {GameComponentTemplates.ToClassName(_newAssetName)}"
                : "Enter base name for the new component:",
            CreateAssetKind.System => isValidName
                ? $"Enter base name for the new system:\nWill create: {GameSystemTemplates.ToClassName(_newAssetName)}"
                : "Enter base name for the new system:",
            _ => "Enter name:"
        };

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (!ImGui.BeginPopupModal(_createAssetPopupName,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
            return;

        ImGui.Text(promptText);

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        var enterPressed = ImGui.InputText($"##{CreateAssetPopupId}_Input", ref _newAssetName,
            EditorUIConstants.MaxNameLength, ImGuiInputTextFlags.EnterReturnsTrue);

        ImGui.Separator();

        if (!isValidName && !string.IsNullOrEmpty(_newAssetName))
            TextDrawer.DrawErrorText("Name must start with a letter and contain only letters, numbers, and underscores.");

        if (!string.IsNullOrEmpty(_errorMessage))
            TextDrawer.DrawErrorText(_errorMessage);

        var shouldClose = false;

        ButtonDrawer.DrawModalButtonPair(
            onOk: () =>
            {
                if (!isValidName)
                    return;
                shouldClose = true;
                _ = CreateAssetAsync();
            },
            onCancel: () =>
            {
                shouldClose = true;
                _errorMessage = null;
            },
            okDisabled: !isValidName);

        if (enterPressed && isValidName)
        {
            shouldClose = true;
            _ = CreateAssetAsync();
        }

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            shouldClose = true;
            _errorMessage = null;
        }

        if (shouldClose)
        {
            _showNameModal = false;
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private async Task CreateAssetAsync()
    {
        try
        {
            var (success, error) = _pendingCreateKind switch
            {
                CreateAssetKind.Component => await _actions.CreateComponentAsync(_newAssetName),
                CreateAssetKind.System => await _actions.CreateSystemAsync(_newAssetName),
                _ => (false, "Unknown asset type.")
            };

            if (success)
            {
                _errorMessage = null;
                return;
            }

            _errorMessage = error ?? "Failed to create asset.";
            _queueCreateAssetModal = true;
            _showNameModal = true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create {Kind} asset", _pendingCreateKind);
            _errorMessage = ex.Message;
            _queueCreateAssetModal = true;
            _showNameModal = true;
        }
    }

    private void DrawContentGrid()
    {
        _thumbnails.Pump();

        ImGui.TextWrapped($"Current Path: {_currentDirectory}");
        ImGui.Separator();

        if (_currentDirectory != _assetPath)
        {
            ButtonDrawer.DrawCompactButton("<-", () =>
            {
                NavigateTo(Directory.GetParent(_currentDirectory)!.FullName);
            });
            ImGui.SameLine();
        }

        LayoutDrawer.DrawSearchInput("Filter...", ref _folderFilter);

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

            if (!string.IsNullOrEmpty(_folderFilter) &&
                !filenameString.Contains(_folderFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            ImGui.PushID(filenameString);

            var (icon, isImage) = ResolveIcon(info, entry, isDirectory);

            ButtonDrawer.DrawTransparentIconButton(
                filenameString,
                icon,
                new Vector2(thumbnailSize, thumbnailSize));

            DragDropDrawer.CreateDragDropSource(
                "CONTENT_BROWSER_ITEM",
                relativePath,
                () => RenderDragDropPreview(filenameString, icon, isImage, isDirectory));

            if (OperatingSystem.IsWindows() && ImGui.BeginPopupContextItem($"ItemCtx##{entry}"))
            {
                if (ImGui.MenuItem("Show in Explorer"))
                    ShowInExplorer(info.FullName);
                if (ImGui.MenuItem("Edit"))
                    Edit(info.FullName);
                ImGui.EndPopup();
            }

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left) &&
                !File.Exists(info.FullName))
            {
                NavigateTo(info.FullName);
            }

            ImGui.TextWrapped(filenameString);
            ImGui.NextColumn();

            ImGui.PopID();
        }

        ImGui.Columns(1);
    }

    private void NavigateTo(string directory)
    {
        _currentDirectory = directory;
        _pendingTreeExpand = true;
        _folderFilter = string.Empty;
        _imageCache.Clear();
        _thumbnails.Clear();
    }

    private void RequestThumbnail(string entry)
    {
        _thumbnails.GetOrRequest(entry);
    }

    private (Texture2D icon, bool isImage) ResolveIcon(FileSystemInfo info, string entry, bool isDirectory)
    {
        if (isDirectory)
        {
            var folderName = info.Name.ToLowerInvariant();
            if (_folderIconCache.TryGetValue(folderName, out var folderIcon))
                return (folderIcon, false);
            return (_directoryIcon, false);
        }

        if (DragDropDrawer.HasValidExtension(info.Name, AssetKind.Texture.Extensions))
        {
            // Shared cache drives decode; this local cache only remembers the
            // resolved icon (texture or _fileIcon fallback for failed decodes).
            var thumb = _thumbnails.GetOrRequest(entry);
            if (thumb is not null)
            {
                _imageCache[entry] = thumb;
                return (thumb, true);
            }

            if (_imageCache.TryGetValue(entry, out var cached))
                return (cached, true);

            RequestThumbnail(entry);
            return (_fileIcon, true);
        }

        return (_fileIcon, false);
    }

    private static void RenderDragDropPreview(string filename, Texture icon, bool isImage, bool isDirectory)
    {
        ImGui.Text($"Dragging: {filename}");
        if (isImage)
        {
            TextDrawer.DrawInfoText("Type: Texture");
            ImGui.Image(ImGuiNativeTexture.From(icon), new Vector2(32, 32), new Vector2(0, 1), new Vector2(1, 0));
        }
        else if (isDirectory)
            TextDrawer.DrawInfoText("Type: Directory");
        else
            TextDrawer.DrawInfoText($"Type: {Path.GetExtension(filename)}");
    }

    private static void ShowInExplorer(string path)
    {
        try
        {
            Process.Start("explorer.exe", $"/select,\"{path}\"");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to show in Explorer: {Path}", path);
        }
    }

    private static void Edit(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to edit: {Path}", path);
        }
    }

    public void SetRootDirectory(string rootDir)
    {
        _assetPath = rootDir;
        NavigateTo(rootDir);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        // Shared AssetThumbnailCache is owned by the editor lifecycle; do not dispose here.
        _imageCache.Clear();
    }
}
