using System.Numerics;
using ECS;
using Engine.Scene.Components;
using Engine.Scripting;
using ImGuiNET;
using NLog;

namespace Editor.Panels;

public static class ScriptComponentUI
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private static readonly ScriptEditorWindow ScriptEditor = new ScriptEditorWindow();

    private static bool _showCreateScriptPopup = false;
    private static bool _showScriptSelectorPopup = false;
    private static string _newScriptName = string.Empty;
    private static Entity? _selectedEntity = null;

    public static void Render()
    {
        // Render the script editor window if open
        ScriptEditor.Render();

        // Render popup dialogs
        RenderCreateScriptPopup();
        RenderScriptSelectorPopup();
    }

    public static void DrawScriptComponent(Entity entity)
    {
        _selectedEntity = entity;

        DrawComponent<NativeScriptComponent>("Script", entity, component =>
        {
            var script = component.ScriptableEntity;

            // Display current script info
            if (script != null)
            {
                var scriptType = script.GetType();
                ImGui.TextColored(new Vector4(1, 1, 0, 1), $"Script: {scriptType.Name}");
                if (ImGui.BeginPopupContextItem($"ScriptContextMenu_{scriptType.Name}"))
                {
                    if (ImGui.MenuItem("Edit"))
                    {
                        ScriptEditor.Open(scriptType.Name);
                    }
                    if (ImGui.MenuItem("Remove"))
                    {
                        entity.RemoveComponent<NativeScriptComponent>();
                    }
                    ImGui.EndPopup();
                }
                var fields = script.GetExposedFields().ToList();
                if (!fields.Any())
                {
                    ImGui.TextColored(new Vector4(1, 0, 0, 1), "No public fields/properties found!");
                }
                foreach (var (fieldName, fieldType, fieldValue) in fields)
                {
                    bool changed = false;
                    object newValue = fieldValue;
                    string inputLabel = $"{fieldName}##{fieldName}"; // Unique label for ImGui controls

                    if (fieldType == typeof(int))
                    {
                        int v = (int)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.DragInt(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(float))
                    {
                        float v = (float)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.DragFloat(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(double))
                    {
                        float v = (float)(double)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.DragFloat(inputLabel, ref v))
                        {
                            newValue = (double)v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(bool))
                    {
                        bool v = (bool)fieldValue;
                        if (ImGui.Checkbox(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(string))
                    {
                        string v = (string)fieldValue ?? string.Empty;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.InputText(inputLabel, ref v, 256))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(Vector2))
                    {
                        Vector2 v = (Vector2)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.DragFloat2(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(Vector3))
                    {
                        Vector3 v = (Vector3)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.DragFloat3(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    else if (fieldType == typeof(Vector4))
                    {
                        Vector4 v = (Vector4)fieldValue;
                        ImGui.SetNextItemWidth(120);
                        if (ImGui.ColorEdit4(inputLabel, ref v))
                        {
                            newValue = v;
                            changed = true;
                        }
                    }
                    if (changed)
                    {
                        script.SetFieldValue(fieldName, newValue);
                    }
                }
                // --- End editable fields ---
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0, 0, 1), "No script instance attached!");
            }

            ImGui.Separator();

            // Add Script buttons
            if (ImGui.Button("Add Existing Script", new Vector2(150, 0)))
            {
                _showScriptSelectorPopup = true;
            }

            ImGui.SameLine();
            if (ImGui.Button("Create New Script", new Vector2(150, 0)))
            {
                _showCreateScriptPopup = true;
                _newScriptName = $"Script_{DateTime.Now.Ticks % 1000:000}";
            }
        });
    }

    private static void RenderCreateScriptPopup()
    {
        if (_showCreateScriptPopup)
        {
            ImGui.OpenPopup("Create New Script");
        }

        // Always center this window when appearing
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Create New Script", ref _showCreateScriptPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter name for the new script:");
            ImGui.InputText("##ScriptName", ref _newScriptName, 100);

            ImGui.Separator();

            // Validate script name
            bool isValidName = !string.IsNullOrEmpty(_newScriptName) &&
                               System.Text.RegularExpressions.Regex.IsMatch(_newScriptName, @"^[a-zA-Z][a-zA-Z0-9_]*$");

            if (!isValidName)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(
                    "Script name must start with a letter and contain only letters, numbers, and underscores.");
                ImGui.PopStyleColor();
            }

            ImGui.BeginDisabled(!isValidName);
            if (ImGui.Button("Create and Edit", new Vector2(120, 0)))
            {
                _showCreateScriptPopup = false;
                ScriptEditor.Open(_newScriptName, true, success =>
                {
                    if (success && _selectedEntity != null)
                    {
                        try
                        {
                            var scriptInstanceResult = ScriptEngine.Instance.CreateScriptInstance(_newScriptName);
                            if (scriptInstanceResult.IsSuccess)
                            {
                                var scriptInstance = scriptInstanceResult.Value;
                                if (_selectedEntity.HasComponent<NativeScriptComponent>())
                                {
                                    _selectedEntity.GetComponent<NativeScriptComponent>().ScriptableEntity =
                                        scriptInstance;
                                }
                                else
                                {
                                    _selectedEntity.AddComponent(new NativeScriptComponent
                                    {
                                        ScriptableEntity = scriptInstance
                                    });
                                }

                                Logger.Info($"Added script {_newScriptName} to entity {_selectedEntity.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, $"Failed to create script instance for {_newScriptName}");
                        }
                    }
                });
            }

            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showCreateScriptPopup = false;
            }

            ImGui.EndPopup();
        }
    }

    private static void RenderScriptSelectorPopup()
    {
        if (_showScriptSelectorPopup)
        {
            ImGui.OpenPopup("Select Script");
        }

        // Always center this window when appearing
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Select Script", ref _showScriptSelectorPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Select a script to attach:");

            var availableScripts = ScriptEngine.Instance.GetAvailableScriptNames();

            if (availableScripts.Length == 0)
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), "No scripts available. Create one first!");
            }
            else
            {
                // Calculate proper height for the listbox
                float itemHeight = ImGui.GetTextLineHeightWithSpacing();
                int visibleItems = Math.Min(availableScripts.Length, 10);
                float listboxHeight = itemHeight * visibleItems + ImGui.GetStyle().FramePadding.Y * 2;

                ImGui.BeginChild("ScriptsList", new Vector2(300, listboxHeight));

                for (int i = 0; i < availableScripts.Length; i++)
                {
                    string scriptName = availableScripts[i];

                    if (ImGui.Selectable(scriptName, false, ImGuiSelectableFlags.DontClosePopups))
                    {
                        _showScriptSelectorPopup = false;

                        if (_selectedEntity != null)
                        {
                            try
                            {
                                var scriptInstanceResult = ScriptEngine.Instance.CreateScriptInstance(scriptName);
                                if (scriptInstanceResult.IsSuccess)
                                {
                                    var scriptInstance = scriptInstanceResult.Value;
                                    if (_selectedEntity.HasComponent<NativeScriptComponent>())
                                    {
                                        _selectedEntity.GetComponent<NativeScriptComponent>().ScriptableEntity =
                                            scriptInstance;
                                    }
                                    else
                                    {
                                        _selectedEntity.AddComponent(new NativeScriptComponent
                                        {
                                            ScriptableEntity = scriptInstance
                                        });
                                    }

                                    Logger.Info($"Added script {scriptName} to entity {_selectedEntity.Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, $"Failed to create script instance for {scriptName}");
                            }
                        }
                    }

                    if (ImGui.IsItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) // Right-click
                    {
                        ImGui.OpenPopup($"ScriptContextMenu_{i}");
                    }

                    if (ImGui.BeginPopup($"ScriptContextMenu_{i}"))
                    {
                        if (ImGui.MenuItem("Edit"))
                        {
                            ScriptEditor.Open(scriptName);
                            ImGui.CloseCurrentPopup();
                        }

                        if (ImGui.MenuItem("Delete"))
                        {
                            if (ScriptEngine.Instance.DeleteScript(scriptName))
                            {
                                Logger.Info($"Deleted script {scriptName}");
                            }

                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }
                }

                ImGui.EndChild();
            }

            ImGui.Separator();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showScriptSelectorPopup = false;
            }

            ImGui.EndPopup();
        }
    }

    private static void DrawComponent<T>(string name, Entity entity, Action<T> uiFunction) where T : Component
    {
        // Similar to your existing DrawComponent method in SceneHierarchyPanel
        ImGuiTreeNodeFlags treeNodeFlags = ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed
                                                                          | ImGuiTreeNodeFlags.SpanAvailWidth |
                                                                          ImGuiTreeNodeFlags.AllowOverlap |
                                                                          ImGuiTreeNodeFlags.FramePadding;

        if (entity.HasComponent<T>())
        {
            T component = entity.GetComponent<T>();
            Vector2 contentRegionAvailable = ImGui.GetContentRegionAvail();

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
            float lineHeight = ImGui.GetFont().FontSize + ImGui.GetStyle().FramePadding.Y * 2.0f;
            ImGui.Separator();

            bool open = ImGui.TreeNodeEx(typeof(T).GetHashCode().ToString(), treeNodeFlags, name);
            ImGui.PopStyleVar();

            ImGui.SameLine(contentRegionAvailable.X - lineHeight * 0.5f);
            if (ImGui.Button("+", new Vector2(lineHeight, lineHeight)))
            {
                ImGui.OpenPopup("ComponentSettings");
            }

            bool removeComponent = false;
            if (ImGui.BeginPopup("ComponentSettings"))
            {
                if (ImGui.MenuItem("Remove component"))
                    removeComponent = true;
                ImGui.EndPopup();
            }

            if (open)
            {
                uiFunction(component);
                ImGui.TreePop();
            }

            if (removeComponent)
                entity.RemoveComponent<T>();
        }
        else
        {
            // If entity doesn't have this component, we'll create a placeholder for adding it
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 4));
            ImGui.Separator();

            // Use different tree node flags for placeholder
            ImGuiTreeNodeFlags placeholderFlags = ImGuiTreeNodeFlags.Framed |
                                                  ImGuiTreeNodeFlags.SpanAvailWidth |
                                                  ImGuiTreeNodeFlags.AllowOverlap;

            bool open = ImGui.TreeNodeEx($"Add{name}Placeholder", placeholderFlags, $"Add {name}");
            ImGui.PopStyleVar();

            if (open)
            {
                // Add NativeScriptComponent button
                if (ImGui.Button($"Add {name} Component", new Vector2(ImGui.GetContentRegionAvail().X, 0)))
                {
                    entity.AddComponent(new NativeScriptComponent());

                    // After adding, call UI function with newly created component
                    uiFunction(entity.GetComponent<T>());
                }

                ImGui.TreePop();
            }
        }
    }
}