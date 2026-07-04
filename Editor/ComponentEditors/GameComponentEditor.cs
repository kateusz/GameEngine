using ECS;
using Editor.Features.Components;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Scripting;
using ImGuiNET;
using Serilog;

namespace Editor.ComponentEditors;

public class GameComponentEditor(IGameComponentFactory gameComponentFactory)
{
    private static readonly ILogger Logger = Log.ForContext<GameComponentEditor>();

    private bool _showChoicePopup;
    private bool _showAttachPopup;
    private bool _showCreatePopup;
    private string _newComponentBaseName = string.Empty;
    private Entity? _selectedEntity;
    private string? _errorMessage;

    public void RequestCreate(Entity entity)
    {
        _selectedEntity = entity;
        _newComponentBaseName = string.Empty;
        _errorMessage = null;
        _showChoicePopup = true;
    }

    public void RenderPopups()
    {
        RenderChoicePopup();
        RenderAttachPopup();
        RenderCreatePopup();
    }

    private void RenderChoicePopup()
    {
        if (!ModalDrawer.BeginCenteredModal("Game Component", ref _showChoicePopup))
            return;

        ImGui.Text("Attach an existing component or create a new one?");
        ImGui.TextDisabled("Serializable state — use Game Systems for rules and input.");
        ImGui.Separator();

        if (ButtonDrawer.DrawModalButton("Attach Existing"))
        {
            _showChoicePopup = false;
            _errorMessage = null;
            _showAttachPopup = true;
        }

        ImGui.SameLine();

        if (ButtonDrawer.DrawModalButton("Create New"))
        {
            _showChoicePopup = false;
            _errorMessage = null;
            _newComponentBaseName = string.Empty;
            _showCreatePopup = true;
        }

        ImGui.SameLine();

        if (ButtonDrawer.DrawModalButton("Cancel") || ImGui.IsKeyPressed(ImGuiKey.Escape))
            _showChoicePopup = false;

        ModalDrawer.EndModal();
    }

    private void RenderAttachPopup()
    {
        var available = gameComponentFactory.DiscoverComponentNames();

        ModalDrawer.RenderListSelectionModal(
            title: "Attach Game Component",
            showModal: ref _showAttachPopup,
            items: available,
            onItemSelected: OnComponentSelected,
            onCancel: () => { },
            emptyMessage: "No game components found in scripts. Create one first!");
    }

    private void OnComponentSelected(string typeName)
    {
        if (_selectedEntity is null)
            return;

        try
        {
            var (success, error) = gameComponentFactory.AttachExisting(_selectedEntity, typeName);
            if (success)
                return;

            Logger.Error("Failed to attach game component {Name}: {Error}", typeName, error);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to attach game component {Name}", typeName);
        }
    }

    private void RenderCreatePopup()
    {
        var isValidName = !string.IsNullOrEmpty(_newComponentBaseName) &&
                          System.Text.RegularExpressions.Regex.IsMatch(_newComponentBaseName, @"^[a-zA-Z][a-zA-Z0-9_]*$");

        var validationMessage = !isValidName && !string.IsNullOrEmpty(_newComponentBaseName)
            ? "Name must start with a letter and contain only letters, numbers, and underscores."
            : null;

        var className = isValidName ? GameComponentTemplates.ToClassName(_newComponentBaseName) : null;
        var promptText = className is null
            ? "Enter base name for the new component:"
            : $"Enter base name for the new component:\nWill create: {className}";

        ModalDrawer.RenderInputModal(
            title: "Create Game Component",
            showModal: ref _showCreatePopup,
            promptText: promptText,
            inputValue: ref _newComponentBaseName,
            maxLength: EditorUIConstants.MaxNameLength,
            validationMessage: validationMessage,
            errorMessage: _errorMessage,
            isValid: isValidName,
            onOk: async () =>
            {
                if (_selectedEntity is null)
                {
                    _errorMessage = "No entity selected.";
                    _showCreatePopup = true;
                    return;
                }

                try
                {
                    var (success, error) = await gameComponentFactory.CreateAndAttachAsync(
                        _selectedEntity, _newComponentBaseName);
                    if (success)
                        return;

                    _errorMessage = error;
                    _showCreatePopup = true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to create game component {Name}", _newComponentBaseName);
                    _errorMessage = ex.Message;
                    _showCreatePopup = true;
                }
            },
            onCancel: () => _errorMessage = null,
            okLabel: "Create");
    }
}
