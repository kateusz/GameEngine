using System.Numerics;
using System.Text.RegularExpressions;
using Engine.Core;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Renderer.Textures;
using Engine.Scripting;
using ImGuiNET;
using Serilog;

namespace Editor.Panels;

public class ContentBrowserPanel : IContentBrowserPanel, IEditorPanel
{
    private enum CreateAssetKind { Script, Component, System }

    private const float TreePanelWidth = 200f;
    private static readonly Regex ValidNameRegex = new(@"^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
    private static readonly ILogger Logger = Log.ForContext<ContentBrowserPanel>();

    private readonly ITextureFactory _textureFactory;
    private readonly IProjectContext _projectContext;
    private readonly ContentBrowserActions _actions;
    private string _assetPath;
    private string _currentDirectory;
    private Texture2D _directoryIcon = null!;
    private Texture2D _fileIcon = null!;
    private readonly Dictionary<string, Texture2D> _imageCache = new();
    private readonly Dictionary<string, Texture2D> _folderIconCache = new();

    private const string CreateAssetPopupId = "ContentBrowserCreateAsset";

    private CreateAssetKind _pendingCreateKind;
    private bool _showNameModal;
    private bool _queueCreateAssetModal;
    private string _createAssetPopupName = string.Empty;
    private string _newAssetName = string.Empty;
    private string? _errorMessage;

    public ContentBrowserPanel(
        ITextureFactory textureFactory,
        IProjectContext projectContext,
        ContentBrowserActions actions)
    {
        _textureFactory = textureFactory;
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

    public void RenderPopups() => RenderCreateAssetModal();

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
        
        var opened = ImGui.TreeNodeEx($"{dirName}##{directoryPath}", flags);

        if (ImGui.IsItemClicked())
            _currentDirectory = directoryPath;

        if (ImGui.BeginPopupContextItem($"DirCtx##{directoryPath}"))
        {
            var canCreate = CanCreateScriptAssets(directoryPath);
            if (ImGui.MenuItem("Add Script", enabled: canCreate))
                BeginCreateAsset(CreateAssetKind.Script);
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
            CreateAssetKind.Script => $"Script_{DateTime.Now.Ticks % 1000:000}",
            CreateAssetKind.Component => string.Empty,
            CreateAssetKind.System => "MyGame",
            _ => string.Empty
        };
        _createAssetPopupName = kind switch
        {
            CreateAssetKind.Script => $"Create New Script##{CreateAssetPopupId}",
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
            CreateAssetKind.Script => "Enter name for the new script:",
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
                CreateAssetKind.Script => await _actions.CreateScriptAsync(_newAssetName),
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
