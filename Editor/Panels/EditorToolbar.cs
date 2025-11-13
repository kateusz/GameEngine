using System.Numerics;
using Engine.Renderer.Textures;
using Engine.Scene;
using ImGuiNET;

namespace Editor.Panels;

public class EditorToolbar
{
    private Texture2D _iconPlay;
    private Texture2D? _iconPause;
    private Texture2D _iconStop;
    private Texture2D? _iconRestart;
    private Texture2D _iconSelect;
    private Texture2D _iconMove;
    private Texture2D _iconScale;
    private readonly ISceneManager _sceneManager;

    public EditorMode CurrentMode { get; set; } = EditorMode.Select;

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

        // Try to load pause and restart icons (optional - will fall back to text buttons if not found)
        try
        {
            _iconPause = TextureFactory.Create("Resources/Icons/PauseButton.png");
        }
        catch
        {
            _iconPause = null; // Will use text button fallback
        }

        try
        {
            _iconRestart = TextureFactory.Create("Resources/Icons/RestartButton.png");
        }
        catch
        {
            _iconRestart = null; // Will use text button fallback
        }
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

        // Center: Play/Pause/Stop/Restart controls
        RenderPlaybackControls(size);

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
        ImGui.End();
    }

    /// <summary>
    /// Renders the playback controls (Play/Pause/Stop/Restart) in the center of the toolbar.
    /// </summary>
    private void RenderPlaybackControls(float size)
    {
        float centerX = ImGui.GetWindowContentRegionMax().X * 0.5f;
        float buttonSize = 20.0f;
        float spacing = 5.0f;

        // Calculate total width based on visible buttons
        // Edit mode: 1 button (Play)
        // Play/Paused mode: 3 buttons (Pause/Play, Restart, Stop)
        int buttonCount = _sceneManager.SceneState == SceneState.Edit ? 1 : 3;
        float totalWidth = (buttonSize * buttonCount) + (spacing * (buttonCount - 1));
        float startX = centerX - (totalWidth * 0.5f);

        ImGui.SetCursorPosX(startX);
        ImGui.SetCursorPosY(2.0f);

        // Play/Pause button (context-sensitive)
        if (_sceneManager.SceneState == SceneState.Edit)
        {
            // Show Play button in Edit mode
            if (ImGui.ImageButton("play", (IntPtr)_iconPlay.GetRendererId(), new Vector2(buttonSize, buttonSize)))
            {
                _sceneManager.Play();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Play (F5)");
        }
        else
        {
            // Show Pause or Play button depending on state
            if (_sceneManager.SceneState == SceneState.Play)
            {
                // Playing: show pause button
                if (_iconPause != null)
                {
                    if (ImGui.ImageButton("pause", (IntPtr)_iconPause.GetRendererId(), new Vector2(buttonSize, buttonSize)))
                    {
                        _sceneManager.Pause();
                    }
                }
                else
                {
                    // Fallback to text button
                    if (ImGui.Button("⏸", new Vector2(buttonSize, buttonSize)))
                    {
                        _sceneManager.Pause();
                    }
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Pause (Ctrl+P)");
            }
            else // Paused
            {
                // Paused: show play button to resume
                if (ImGui.ImageButton("resume", (IntPtr)_iconPlay.GetRendererId(), new Vector2(buttonSize, buttonSize)))
                {
                    _sceneManager.Resume();
                }
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Resume (Ctrl+P)");
            }

            // Restart button (only visible when playing or paused)
            ImGui.SameLine(0, spacing);

            if (_iconRestart != null)
            {
                if (ImGui.ImageButton("restart", (IntPtr)_iconRestart.GetRendererId(), new Vector2(buttonSize, buttonSize)))
                {
                    _sceneManager.Restart();
                }
            }
            else
            {
                // Fallback to text button
                if (ImGui.Button("🔄", new Vector2(buttonSize, buttonSize)))
                {
                    _sceneManager.Restart();
                }
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Restart (Ctrl+R)");

            // Stop button (only visible when playing or paused)
            ImGui.SameLine(0, spacing);

            if (ImGui.ImageButton("stop", (IntPtr)_iconStop.GetRendererId(), new Vector2(buttonSize, buttonSize)))
            {
                _sceneManager.Stop();
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Stop (Shift+F5)");
        }
    }
}