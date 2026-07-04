using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using ECS;
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
    ISceneContext sceneContext)
{
    private static readonly ILogger Logger = Log.ForContext(typeof(ScriptComponentEditor));

    private bool _showCreateScriptPopup;
    private bool _showScriptSelectorPopup;
    private string _newScriptName = string.Empty;
    private Entity _selectedEntity;

    public void Draw()
    {
        RenderCreateScriptPopup();
        RenderScriptSelectorPopup();
    }

    public void DrawScriptComponent(Entity entity)
    {
        _selectedEntity = entity;

        DrawComponent<NativeScriptComponent>("Script", entity, component =>
        {
            if (!string.IsNullOrWhiteSpace(component.ScriptTypeName))
                DrawAttachedScript(entity, component);
            else
                DrawNoScriptMessage();

            ImGui.Separator();
            DrawScriptActions();
        });
    }

    private void DrawAttachedScript(Entity entity, NativeScriptComponent component)
    {
        ImGui.TextDisabled("Per-entity glue — put data in Game Components, batch logic in Game Systems.");
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
                entity.RemoveComponent<NativeScriptComponent>();
                if (sceneContext.ActiveScriptRuntimeStore is { } store)
                    scriptWorkspace.ForceRecompile(store);
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
                    var scriptTemplate = scriptWorkspace.GenerateScriptTemplate(_newScriptName);
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
                            _selectedEntity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
                            {
                                ScriptTypeName = _newScriptName
                            });
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
                _selectedEntity.AddComponent<NativeScriptComponent>(new NativeScriptComponent { ScriptTypeName = scriptName });

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

    private static void DrawComponent<T>(string name, Entity entity, Action<T> uiFunction) where T : IComponent
    {
        // Similar to your existing DrawComponent method in SceneHierarchyPanel
        var treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                           | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                           ImGuiTreeNodeFlags.AllowOverlap |
                                                           ImGuiTreeNodeFlags.FramePadding;

        if (entity.TryGetComponent<T>(out var component))
        {
            var contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
            var lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            var open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            ButtonDrawer.DrawButton("-", lineHeight, lineHeight, () => entity.RemoveComponent<T>());

            if (open)
            {
                uiFunction(component);
                ImGui.TreePop();
            }
        }
        else
        {
            // If entity doesn't have this component, we'll create a placeholder for adding it
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
            ImGui.Separator();

            // Use different tree node flags for placeholder
            var placeholderFlags = ImGuiTreeNodeFlags.Framed |
                                   ImGuiTreeNodeFlags.SpanAvailWidth |
                                   ImGuiTreeNodeFlags.AllowOverlap;

            var open = ImGui.TreeNodeEx($"Add{name}Placeholder", placeholderFlags, $"Add {name}");
            ImGui.PopStyleVar();

            if (!open) 
                return;
            
            // Add NativeScriptComponent button
            ButtonDrawer.DrawFullWidthButton($"Add {name} Component", () =>
            {
                entity.AddComponent<NativeScriptComponent>(new NativeScriptComponent());

                // After adding, call UI function with newly created component
                uiFunction(entity.GetComponent<T>());
            });

            ImGui.TreePop();
        }
    }
}