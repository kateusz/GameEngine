using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace Editor.Features.Scene;

public class SceneToolbar(ISceneContext sceneContext, ISceneManager sceneManager, ITextureFactory textureFactory)
{
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private Texture2D _iconSelect;
    private Texture2D _iconMove;
    private Texture2D _iconScale;
    private Texture2D _iconRotate;
    private Texture2D _iconRuler;
    private Texture2D _iconRestart;

    public EditorMode CurrentMode { get; set; } = EditorMode.Select;

    public void Init()
    {
        _iconPlay = textureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = textureFactory.Create("Resources/Icons/StopButton.png");
        _iconSelect = textureFactory.Create("Resources/Icons/select.png");
        _iconMove = textureFactory.Create("Resources/Icons/move.png");
        _iconScale = textureFactory.Create("Resources/Icons/scale.png");
        _iconRotate = textureFactory.Create("Resources/Icons/rotate.png");
        _iconRuler = textureFactory.Create("Resources/Icons/ruler.png");
        _iconRestart = textureFactory.Create("Resources/Icons/restart.png");
    }

    public void Render()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 2));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(0, 0));
        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));

        var colors = ImGui.GetStyle().Colors;
        var buttonHovered = colors[(int)ImGuiCol.ButtonHovered];
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, buttonHovered with { W = 0.5f });

        var buttonActive = colors[(int)ImGuiCol.ButtonActive];
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, buttonActive with { W = 0.5f });

        ImGui.Begin("##toolbar",
            ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var size = ImGui.GetWindowHeight() - 4.0f;

        ImGui.SetCursorPosX(10.0f);

        ButtonDrawer.DrawIconButton("select", _iconSelect, new Vector2(15, 15),
            isSelected: CurrentMode == EditorMode.Select,
            onClick: () => CurrentMode = EditorMode.Select,
            tooltip: "Select Mode");
        ImGui.SameLine();
        ButtonDrawer.DrawIconButton("move", _iconMove, new Vector2(15, 15),
            isSelected: CurrentMode == EditorMode.Move,
            onClick: () => CurrentMode = EditorMode.Move,
            tooltip: "Move Mode");
        ImGui.SameLine();
        ButtonDrawer.DrawIconButton("scale", _iconScale, new Vector2(15, 15),
            isSelected: CurrentMode == EditorMode.Scale,
            onClick: () => CurrentMode = EditorMode.Scale,
            tooltip: "Scale Mode");
        ImGui.SameLine();
        ButtonDrawer.DrawIconButton("rotate", _iconRotate, new Vector2(15, 15),
            isSelected: CurrentMode == EditorMode.Rotate,
            onClick: () => CurrentMode = EditorMode.Rotate,
            tooltip: "Rotate Mode");
        ImGui.SameLine();
        ButtonDrawer.DrawIconButton("ruler", _iconRuler, new Vector2(15, 15),
            isSelected: CurrentMode == EditorMode.Ruler,
            onClick: () => CurrentMode = EditorMode.Ruler,
            tooltip: "Ruler Mode");
        ImGui.SameLine();

        DrawDimensionButton("2D", SceneDimension.TwoD);
        ImGui.SameLine();
        DrawDimensionButton("3D", SceneDimension.ThreeD);

        var icon = sceneContext.State == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));
        ImGui.SetCursorPosY(2.0f);

        _ = ButtonDrawer.DrawTransparentIconButton("playstop", icon, new Vector2(20, 20),
                onClick: () =>
                {
                    switch (sceneContext.State)
                    {
                        case SceneState.Edit:
                            sceneManager.Play();
                            break;
                        case SceneState.Play:
                            sceneManager.Stop();
                            break;
                    }
                },
                tooltip: sceneContext.State == SceneState.Edit ? "Play Scene" : "Stop Scene");

        ImGui.SameLine();

        _ = ButtonDrawer.DrawTransparentIconButton("restart", _iconRestart, new Vector2(20, 20),
                onClick: sceneManager.Restart,
                tooltip: "Restart Scene");

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }

    private void DrawDimensionButton(string label, SceneDimension dimension)
    {
        var selected = sceneContext.ActiveScene?.Dimension == dimension;
        if (selected)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.5f, 0.8f, 0.7f));

        ButtonDrawer.DrawButton(label, EditorUIConstants.ToolbarToggleWidth, EditorUIConstants.ToolbarToggleHeight,
            () =>
            {
                if (sceneContext.ActiveScene is { } scene)
                    scene.Dimension = dimension;
            });

        if (selected)
            ImGui.PopStyleColor();

        LayoutDrawer.DrawTooltip($"{label} Scene");
    }
}
