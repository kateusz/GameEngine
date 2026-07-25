using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
using Editor.ComponentEditors.Core;
using Editor.Features.History;
using Editor.Features.History.Commands;
using Editor.Features.Scripting;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Scene;
using Engine.Scripting;
using ImGuiNET;
using SceneComponents;
using Serilog;

namespace Editor.ComponentEditors;

public class ScriptComponentEditor(
    IScriptEngine scriptEngine,
    GameScriptWorkspace scriptWorkspace,
    ISceneContext sceneContext,
    IEditorHistory history) : IComponentEditor
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ScriptComponentEditor));

    private bool _showCreateScriptPopup;
    private bool _showScriptSelectorPopup;
    private string _newScriptName = string.Empty;
    private Entity? _selectedEntity;

    public void Draw()
    {
        RenderCreateScriptPopup();
        RenderScriptSelectorPopup();
    }

    public void DrawComponent(Entity entity)
    {
        _selectedEntity = entity;

        if (entity.TryGetComponent<NativeScriptComponent>(out _))
        {
            ComponentEditorRegistry.DrawComponent<NativeScriptComponent>("Script", entity, history, () =>
            {
                var component = entity.GetComponent<NativeScriptComponent>();
                if (!string.IsNullOrWhiteSpace(component.ScriptTypeName))
                    DrawAttachedScript(entity, component);
                else
                    DrawNoScriptMessage();

                ImGui.Separator();
                DrawScriptActions();
            });
        }
        else
        {
            DrawAddScriptPlaceholder(entity);
        }
    }

    private void DrawAddScriptPlaceholder(Entity entity)
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
        ImGui.Separator();

        var placeholderFlags = ImGuiTreeNodeFlags.Framed
                               | ImGuiTreeNodeFlags.SpanAvailWidth
                               | ImGuiTreeNodeFlags.AllowOverlap;

        var open = ImGui.TreeNodeEx("AddScriptPlaceholder", placeholderFlags, "Add Script");
        ImGui.PopStyleVar();

        if (!open)
            return;

        ButtonDrawer.DrawFullWidthButton("Add Script Component", () =>
            history.Execute(new AddComponentCommand(entity, new NativeScriptComponent())));

        ImGui.TreePop();
    }

    private void DrawAttachedScript(Entity entity, NativeScriptComponent component)
    {
        DrawScriptHeader(entity, component.ScriptTypeName!);
    }

    private void DrawScriptHeader(Entity entity, string scriptTypeName)
    {
        TextDrawer.DrawWarningText($"Script: {scriptTypeName}");

        ImGui.SameLine();
        ButtonDrawer.DrawButton($"Edit##{scriptTypeName}", 0, 0, () => OpenScriptInExternalEditor(scriptTypeName));

        if (ImGui.BeginPopupContextItem($"ScriptContextMenu_{scriptTypeName}"))
        {
            if (ImGui.MenuItem("Remove"))
            {
                history.Execute(new RemoveComponentCommand(entity, typeof(NativeScriptComponent)));
                if (sceneContext is { ActiveScene: { } scene, ActiveScriptRuntimeStore: { } store })
                    scriptWorkspace.ForceRecompile(scene.Context, store);
            }

            ImGui.EndPopup();
        }
    }

    private void OpenScriptInExternalEditor(string scriptName)
    {
        var filePath = scriptWorkspace.GetScriptFilePath(scriptName);
        if (filePath == null)
        {
            Logger.Warning("Script file not found for {ScriptName}", scriptName);
            return;
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                Process.Start("open", filePath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open script {ScriptName} in external editor", scriptName);
        }
    }

    private static void DrawNoScriptMessage()
    {
        TextDrawer.DrawErrorText("No script instance attached!");
    }

    private void DrawScriptActions()
    {
        ButtonDrawer.DrawButton("Add Existing Script", EditorUIConstants.WideButtonWidth, 0,
            () => _showScriptSelectorPopup = true);

        ImGui.SameLine();

        ButtonDrawer.DrawButton("Create New Script", EditorUIConstants.WideButtonWidth, 0, () =>
        {
            _showCreateScriptPopup = true;
            _newScriptName = $"Script_{DateTime.Now.Ticks % 1000:000}";
        });
    }

    private void RenderCreateScriptPopup()
    {
        var isValidName = !string.IsNullOrEmpty(_newScriptName) &&
                          System.Text.RegularExpressions.Regex.IsMatch(_newScriptName, @"^[a-zA-Z][a-zA-Z0-9_]*$");

        var validationMessage = !isValidName
            ? "Script name must start with a letter and contain only letters, numbers, and underscores."
            : null;

        ModalDrawer.RenderInputModal(
            title: "Create New Script",
            showModal: ref _showCreateScriptPopup,
            promptText: "Enter name for the new script:",
            inputValue: ref _newScriptName,
            maxLength: EditorUIConstants.MaxNameLength,
            validationMessage: validationMessage,
            errorMessage: null,
            isValid: isValidName,
            onOk: async () =>
            {
                if (_selectedEntity == null)
                {
                    Logger.Warning("No entity selected for script attachment");
                    return;
                }

                try
                {
                    var scriptTemplate = ScriptableEntityTemplates.Generate(_newScriptName);
                    var (success, errors) = await scriptWorkspace.CreateOrUpdateScriptAsync(_newScriptName, scriptTemplate);

                    if (!success)
                    {
                        Logger.Error("Failed to create script {ScriptName}: {Errors}", _newScriptName, string.Join(", ", errors));
                        return;
                    }

                    var scriptInstanceResult = scriptEngine.CreateScriptInstance(_newScriptName);
                    if (scriptInstanceResult.IsSuccess)
                    {
                        if (_selectedEntity.TryGetComponent<NativeScriptComponent>(out var scriptComponent))
                        {
                            scriptComponent.ScriptTypeName = _newScriptName;
                        }
                        else
                        {
                            // I3 sibling: script .cs file is not undone; only component attachment
                            history.Execute(new AddComponentCommand(_selectedEntity, new NativeScriptComponent
                            {
                                ScriptTypeName = _newScriptName
                            }));
                        }

                        Logger.Information("Created and attached script {ScriptName} to entity {EntityName}", _newScriptName, _selectedEntity.Name);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to create script {ScriptName}", _newScriptName);
                }
            },
            onCancel: () => { },
            okLabel: "Create");
    }

    private void RenderScriptSelectorPopup()
    {
        var availableScripts = scriptWorkspace.GetAvailableScriptNames();

        ModalDrawer.RenderListSelectionModal(
            title: "Select Script",
            showModal: ref _showScriptSelectorPopup,
            items: availableScripts,
            onItemSelected: OnScriptSelected,
            onCancel: () => { },
            emptyMessage: "No scripts available. Create one first!",
            renderItem: RenderScriptItem);
    }

    private void OnScriptSelected(string scriptName)
    {
        if (_selectedEntity == null) return;
        try
        {
            var scriptInstanceResult = scriptEngine.CreateScriptInstance(scriptName);
            if (!scriptInstanceResult.IsSuccess)
            {
                Logger.Error("Failed to create script instance for {ScriptName}: {Error}", scriptName, scriptInstanceResult.Error);
                return;
            }

            if (_selectedEntity.TryGetComponent<NativeScriptComponent>(out var scriptComponent))
                scriptComponent.ScriptTypeName = scriptName;
            else
                history.Execute(new AddComponentCommand(_selectedEntity,
                    new NativeScriptComponent { ScriptTypeName = scriptName }));

            Logger.Information("Added script {ScriptName} to entity {EntityName}", scriptName, _selectedEntity.Name);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to create script instance for {ScriptName}", scriptName);
        }
    }

    private bool RenderScriptItem(string scriptName, int i)
    {
        var itemClicked = ImGui.Selectable(scriptName, false, ImGuiSelectableFlags.DontClosePopups);

        if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup($"ScriptContextMenu_{i}");

        if (ImGui.BeginPopup($"ScriptContextMenu_{i}"))
        {
            if (ImGui.MenuItem("Delete"))
            {
                if (scriptWorkspace.DeleteScript(scriptName))
                    Logger.Information("Deleted script {ScriptName}", scriptName);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }

        return itemClicked;
    }
}