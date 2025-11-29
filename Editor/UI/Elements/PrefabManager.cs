using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Core;
using Engine.Scene.Serializer;
using Serilog;

namespace Editor.UI.Elements;

public class PrefabManager(IPrefabSerializer serializer, IAssetsManager assetsManager) : IPrefabManager
{
    private static readonly ILogger Logger = Log.ForContext<PrefabManager>();

    private bool _showSavePrefabPopup;
    private string _prefabName = "";
    private string _prefabSaveError = "";
    private Entity _entityToSave;

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
        var isValid = !string.IsNullOrWhiteSpace(_prefabName) &&
                      System.Text.RegularExpressions.Regex.IsMatch(_prefabName, @"^[a-zA-Z0-9_\- ]+$");

        var validationMessage = (!isValid && !string.IsNullOrEmpty(_prefabName))
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
                    serializer.SerializeToPrefab(_entityToSave, _prefabName, currentProjectPath);
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
        var assetsPath = assetsManager.AssetsPath;
        return Path.GetDirectoryName(assetsPath) ?? Environment.CurrentDirectory;
    }
}