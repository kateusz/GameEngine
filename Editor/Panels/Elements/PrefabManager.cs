using System.Numerics;
using ECS;
using Engine.Scene.Serializer;
using ImGuiNET;
using NLog;

namespace Editor.Panels.Elements;

public class PrefabManager : IPrefabManager
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    private readonly IPrefabSerializer _serializer;
    
    private bool _showSavePrefabPopup = false;
    private string _prefabName = "";
    private string _prefabSaveError = "";
    private Entity _entityToSave;

    public PrefabManager(IPrefabSerializer serializer)
    {
        _serializer = serializer;
    }

    public void ShowSavePrefabDialog(Entity entity)
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
        if (_showSavePrefabPopup)
        {
            ImGui.OpenPopup("Save as Prefab");
        }

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        if (ImGui.BeginPopupModal("Save as Prefab", ref _showSavePrefabPopup,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text("Enter Prefab Name:");
            ImGui.InputText("##PrefabName", ref _prefabName, 100);
            ImGui.Separator();

            bool isValid = !string.IsNullOrWhiteSpace(_prefabName) &&
                           System.Text.RegularExpressions.Regex.IsMatch(_prefabName, @"^[a-zA-Z0-9_\- ]+$");

            if (!isValid && !string.IsNullOrEmpty(_prefabName))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped("Prefab name must be non-empty and contain only letters, numbers, spaces, dashes, or underscores.");
                ImGui.PopStyleColor();
            }

            if (!string.IsNullOrEmpty(_prefabSaveError))
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.TextWrapped(_prefabSaveError);
                ImGui.PopStyleColor();
            }

            ImGui.BeginDisabled(!isValid);
            if (ImGui.Button("Save", new Vector2(120, 0)))
            {
                try
                {
                    var currentProjectPath = GetCurrentProjectPath();
                    _serializer.SerializeToPrefab(_entityToSave, _prefabName, currentProjectPath);
                    Logger.Info("Saved prefab: {PrefabName}.prefab", _prefabName);
                    _showSavePrefabPopup = false;
                    _prefabSaveError = "";
                }
                catch (Exception ex)
                {
                    _prefabSaveError = $"Failed to save prefab: {ex.Message}";
                }
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showSavePrefabPopup = false;
                _prefabSaveError = "";
            }

            ImGui.EndPopup();
        }
    }

    private string GetCurrentProjectPath()
    {
        var assetsPath = AssetsManager.AssetsPath;
        return Path.GetDirectoryName(assetsPath) ?? Environment.CurrentDirectory;
    }
}