using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.UI.Elements;

public class PrefabManager : IPrefabManager
{
    private static readonly Serilog.ILogger Logger = Log.ForContext<PrefabManager>();

    private readonly IPrefabSerializer _serializer;
    private readonly IAssetsManager _assetsManager;

    private bool _showSavePrefabPopup = false;
    private string _prefabName = "";
    private string _prefabSaveError = "";
    private Entity _entityToSave;

    public PrefabManager(IPrefabSerializer serializer, IAssetsManager assetsManager)
    {
        _serializer = serializer;
        _assetsManager = assetsManager;
    }

    public void ShowSavePrefabPopup(Entity entity)
    {
        _entityToSave = entity;
        _prefabName = entity.Name;
        _prefabSaveError = "";
        _showSavePrefabPopup = true;
    }

    public void RenderPopups()
    {
        RenderSavePrefabPopup();
    }

    private void RenderSavePrefabPopup()
    {
        bool isValid = !string.IsNullOrWhiteSpace(_prefabName) &&
                       System.Text.RegularExpressions.Regex.IsMatch(_prefabName, @"^[a-zA-Z0-9_\- ]+$");

        string? validationMessage = (!isValid && !string.IsNullOrEmpty(_prefabName))
            ? "Prefab name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores."
            : null;

        ModalDrawer.RenderInputModal(
            title: "Save as Prefab",
            showModal: ref _showSavePrefabPopup,
            promptText: "Enter Prefab Name:",
            inputValue: ref _prefabName,
            maxLength: EditorUIConstants.MaxNameLength,
            validationMessage: validationMessage,
            errorMessage: _prefabSaveError,
            isValid: isValid,
            onOk: () =>
            {
                try
                {
                    var currentProjectPath = GetCurrentProjectPath();
                    _serializer.SerializeToPrefab(_entityToSave, _prefabName, currentProjectPath);
                    Logger.Information("Saved prefab: {PrefabName}.prefab", _prefabName);
                    _prefabSaveError = "";
                }
                catch (Exception ex)
                {
                    _prefabSaveError = $"Failed to save prefab: {ex.Message}";
                    _showSavePrefabPopup = true; // Keep modal open on error
                }
            },
            onCancel: () =>
            {
                _prefabSaveError = "";
            },
            okLabel: "Save");
    }

    private string GetCurrentProjectPath()
    {
        var assetsPath = _assetsManager.AssetsPath;
        return Path.GetDirectoryName(assetsPath) ?? Environment.CurrentDirectory;
    }
}