using System.Numerics;
using Editor.UI.Drawers;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class EditorToolbar
{
    private readonly ISceneContext _sceneContext;
    private readonly ITextureFactory _textureFactory;

    private Texture2D _iconPlay = null!;
    private Texture2D _iconStop = null!;
    private Texture2D _iconSelect = null!;
    private Texture2D _iconMove = null!;
    private Texture2D _iconScale = null!;

    public EditorToolbar(ISceneContext sceneContext, ITextureFactory textureFactory)
    {
        _sceneContext = sceneContext;
        _textureFactory = textureFactory;
    }

    public event Action OnPlayScene;
    public event Action OnStopScene;
    public event Action OnRestartScene;

    public EditorMode CurrentMode { get; set; } = EditorMode.Select;
    
    public void Init()
    {
        _iconPlay = _textureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = _textureFactory.Create("Resources/Icons/StopButton.png");
        _iconSelect = _textureFactory.Create("Resources/Icons/select.png");
        _iconMove = _textureFactory.Create("Resources/Icons/move.png");
        _iconScale = _textureFactory.Create("Resources/Icons/scale.png");
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
        
        // Left side: Mode selection buttons
        ImGui.SetCursorPosX(10.0f);

        if (ButtonDrawer.DrawIconButton("select", _iconSelect.GetRendererId(), new Vector2(15, 15),
                isSelected: CurrentMode == EditorMode.Select,
                onClick: () => CurrentMode = EditorMode.Select,
                tooltip: "Select Mode"))
        {
            // Mode already set in onClick
        }

        ImGui.SameLine();

        if (ButtonDrawer.DrawIconButton("move", _iconMove.GetRendererId(), new Vector2(15, 15),
                isSelected: CurrentMode == EditorMode.Move,
                onClick: () => CurrentMode = EditorMode.Move,
                tooltip: "Move Mode"))
        {
            // Mode already set in onClick
        }

        ImGui.SameLine();

        if (ButtonDrawer.DrawIconButton("scale", _iconScale.GetRendererId(), new Vector2(15, 15),
                isSelected: CurrentMode == EditorMode.Scale,
                onClick: () => CurrentMode = EditorMode.Scale,
                tooltip: "Scale Mode"))
        {
            // Mode already set in onClick
        }

        ImGui.SameLine();

        // Use toggle button for ruler since we don't have an icon yet
        bool isRulerMode = CurrentMode == EditorMode.Ruler;
        if (ButtonDrawer.DrawToggleButton("📏", "📏", ref isRulerMode, width: 25, height: 19))
        {
            CurrentMode = EditorMode.Ruler;
        }
        
        var icon = _sceneContext.State == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));
        ImGui.SetCursorPosY(2.0f);

        if (ButtonDrawer.DrawTransparentIconButton("playstop", icon.GetRendererId(), new Vector2(20, 20),
                onClick: () =>
                {
                    switch (_sceneContext.State)
                    {
                        case SceneState.Edit:
                            OnPlayScene();
                            break;
                        case SceneState.Play:
                            OnStopScene();
                            break;
                    }
                },
                tooltip: _sceneContext.State == SceneState.Edit ? "Play Scene" : "Stop Scene"))
        {
            // Action handled in onClick
        }

        ImGui.SameLine();

        ButtonDrawer.DrawSmallButton("🔄", onClick: OnRestartScene, tooltip: "Restart Scene");

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }
}