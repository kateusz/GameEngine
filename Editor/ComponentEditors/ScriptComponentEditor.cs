using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.Elements;
using Editor.UI.FieldEditors;
using Engine.Scene;
using Engine.Scene.Components;
using Engine.Scripting;
using ImGuiNET;
using Serilog;

namespace Editor.ComponentEditors;

public class ScriptComponentEditor(IScriptEngine scriptEngine)
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
            if (component.ScriptableEntity != null)
                DrawAttachedScript(entity, component);
            else
                DrawNoScriptMessage();

            ImGui.Separator();
            DrawScriptActions();
        });
    }

    private void DrawAttachedScript(Entity entity, NativeScriptComponent component)
    {
        var script = component.ScriptableEntity!;
        var scriptType = script.GetType();

        DrawScriptHeader(entity, scriptType);
        DrawScriptFields(script);
    }

    private void DrawScriptHeader(Entity entity, Type scriptType)
    {
        TextDrawer.DrawWarningText($"Script: {scriptType.Name}");

        if (ImGui.BeginPopupContextItem($"ScriptContextMenu_{scriptType.Name}"))
        {
            if (ImGui.MenuItem("Remove"))
            {
                entity.RemoveComponent<NativeScriptComponent>();
                scriptEngine.ForceRecompile();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawScriptFields(ScriptableEntity script)
    {
        var fields = script.GetExposedFields().ToList();

        if (!fields.Any())
        {
            TextDrawer.DrawErrorText("No public fields/properties found!");
            return;
        }

        foreach (var (fieldName, fieldType, fieldValue) in fields)
        {
            DrawScriptField(script, fieldName, fieldType, fieldValue);
        }
    }

    private void DrawScriptField(ScriptableEntity script, string fieldName, Type fieldType, object fieldValue)
    {
        UIPropertyRenderer.DrawPropertyRow(fieldName, () =>
        {
            var inputLabel = $"{fieldName}##{fieldName}";

            if (TryDrawFieldEditor(inputLabel, fieldType, fieldValue, out var newValue))
            {
                script.SetFieldValue(fieldName, newValue);
            }
        });
    }

    private bool TryDrawFieldEditor(string label, Type type, object value, out object newValue)
    {
        newValue = value;

        var editor = FieldEditorRegistry.GetEditor(type);
        if (editor != null)
            return editor.Draw(label, value, out newValue);

        // Fallback: unsupported type
        ImGui.TextDisabled($"Unsupported type: {type.Name}");
        return false;
    }

    private void DrawNoScriptMessage()
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
                    var scriptTemplate = scriptEngine.GenerateScriptTemplate(_newScriptName);
                    var (success, errors) = await scriptEngine.CreateOrUpdateScriptAsync(_newScriptName, scriptTemplate);

                    if (!success)
                    {
                        Logger.Error("Failed to create script {ScriptName}: {Errors}", _newScriptName, string.Join(", ", errors));
                        return;
                    }

                    var scriptInstanceResult = scriptEngine.CreateScriptInstance(_newScriptName);
                    if (scriptInstanceResult.IsSuccess)
                    {
                        var scriptInstance = scriptInstanceResult.Value;
                        if (_selectedEntity.TryGetComponent<NativeScriptComponent>(out var scriptComponent))
                        {
                            scriptComponent.ScriptableEntity = scriptInstance;
                        }
                        else
                        {
                            _selectedEntity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
                            {
                                ScriptableEntity = scriptInstance
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
        var availableScripts = scriptEngine.GetAvailableScriptNames();

        ModalDrawer.RenderListSelectionModal(
            title: "Select Script",
            showModal: ref _showScriptSelectorPopup,
            items: availableScripts,
            onItemSelected: scriptName =>
            {
                if (_selectedEntity != null)
                {
                    try
                    {
                        var scriptInstanceResult = scriptEngine.CreateScriptInstance(scriptName);
                        if (scriptInstanceResult.IsSuccess)
                        {
                            var scriptInstance = scriptInstanceResult.Value;
                            if (_selectedEntity.TryGetComponent<NativeScriptComponent>(out var scriptComponent))
                            {
                                scriptComponent.ScriptableEntity = scriptInstance;
                            }
                            else
                            {
                                _selectedEntity.AddComponent<NativeScriptComponent>(new NativeScriptComponent
                                {
                                    ScriptableEntity = scriptInstance
                                });
                            }

                            Logger.Information("Added script {ScriptName} to entity {EntityName}", scriptName, _selectedEntity.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Failed to create script instance for {ScriptName}", scriptName);
                    }
                }
            },
            onCancel: () => { },
            emptyMessage: "No scripts available. Create one first!",
            renderItem: (scriptName, i) =>
            {
                var itemClicked = false;

                if (ImGui.Selectable(scriptName, false, ImGuiSelectableFlags.DontClosePopups))
                {
                    itemClicked = true;
                }

                if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
                {
                    ImGui.OpenPopup($"ScriptContextMenu_{i}");
                }

                if (ImGui.BeginPopup($"ScriptContextMenu_{i}"))
                {
                    if (ImGui.MenuItem("Delete"))
                    {
                        if (scriptEngine.DeleteScript(scriptName))
                        {
                            Logger.Information("Deleted script {ScriptName}", scriptName);
                        }
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }

                return itemClicked;
            });
    }

    private void DrawComponent<T>(string name, Entity entity, Action<T> uiFunction) where T : IComponent
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