using System.Numerics;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class EditorToolbar
{
    private Texture2D _iconPlay;
    private Texture2D _iconStop;
    private Texture2D _iconSelect;
    private Texture2D _iconMove;
    private Texture2D _iconScale;
    private readonly SceneManager _sceneManager;
    
    public EditorMode CurrentMode { get; set; } = EditorMode.Select;
    private readonly ISceneManager _sceneManager;

    public EditorToolbar(ISceneManager sceneManager)
    {
        _sceneManager = sceneManager ?? throw new ArgumentNullException(nameof(sceneManager));
    }

    public void Init()
    {
        _iconPlay = TextureFactory.Create("Resources/Icons/PlayButton.png");
        _iconStop = TextureFactory.Create("Resources/Icons/StopButton.png");
        _iconSelect = TextureFactory.Create("Resources/Icons/select.png");
        _iconMove = TextureFactory.Create("Resources/Icons/move.png");
        _iconScale = TextureFactory.Create("Resources/Icons/scale.png");
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
        
        if (CurrentMode == EditorMode.Select)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 0.8f));

        if (ImGui.ImageButton("select", (IntPtr)_iconSelect.GetRendererId(), new Vector2(15, 15), new Vector2(0, 0),
                new Vector2(1, 1), new Vector4(0, 0, 0, 0), new Vector4(255.0f, 255.0f, 255.0f, 255.0f) ))
        {
            CurrentMode = EditorMode.Select;
        }
        
        if (CurrentMode == EditorMode.Select)
            ImGui.PopStyleColor();
        
        ImGui.SameLine();
        
        if (CurrentMode == EditorMode.Move)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 0.8f));
        
        if (ImGui.ImageButton("move", (IntPtr)_iconMove.GetRendererId(), new Vector2(15, 15), new Vector2(0, 0),
                new Vector2(1, 1), new Vector4(0, 0, 0, 0), new Vector4(255.0f, 255.0f, 255.0f, 255.0f) ))
        {
            CurrentMode = EditorMode.Move;
        }
        
        if (CurrentMode == EditorMode.Move)
            ImGui.PopStyleColor();
        
        ImGui.SameLine();
        
        if (CurrentMode == EditorMode.Scale)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 0.8f));
        
        if (ImGui.ImageButton("scale", (IntPtr)_iconScale.GetRendererId(), new Vector2(15, 15), new Vector2(0, 0),
                new Vector2(1, 1), new Vector4(0, 0, 0, 0), new Vector4(255.0f, 255.0f, 255.0f, 255.0f) ))
        {
            CurrentMode = EditorMode.Scale;
        }
        
        if (CurrentMode == EditorMode.Scale)
            ImGui.PopStyleColor();
        
        ImGui.SameLine();
        
        if (CurrentMode == EditorMode.Ruler)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.8f, 0.8f));
        
        // Use text button for ruler since we don't have an icon yet
        if (ImGui.Button("📏", new Vector2(25, 19)))
        {
            CurrentMode = EditorMode.Ruler;
        }
        
        if (CurrentMode == EditorMode.Ruler)
            ImGui.PopStyleColor();

        // Center: Play/Stop button
        var icon = _sceneManager.SceneState == SceneState.Edit ? _iconPlay : _iconStop;

        ImGui.SetCursorPosX((ImGui.GetWindowContentRegionMax().X * 0.5f) - (size * 0.5f));
        ImGui.SetCursorPosY(2.0f);

        if (ImGui.ImageButton("playstop", (IntPtr)icon.GetRendererId(), new Vector2(20, 20), new Vector2(0, 0),
                new Vector2(1, 1)))
        {
            switch (_sceneManager.SceneState)
            {
                case SceneState.Edit:
                    _sceneManager.Play();
                    break;
                case SceneState.Play:
                    _sceneManager.Stop();
                    break;
            }
        }

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }
}