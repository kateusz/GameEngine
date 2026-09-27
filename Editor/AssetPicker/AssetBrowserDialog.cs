using System.Numerics;
using Editor.Platform;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;
using Ui.ImGui;

namespace Editor.AssetPicker;

/// <summary>
/// Asset Browser modal UI. Non-UI state lives in <see cref="AssetPickSession"/>.
/// </summary>
public sealed class AssetBrowserDialog(AssetPickSession session, AssetThumbnailCache thumbnails)
    : IDisposable
{
    private const float CellSize = 72f;
    private const float CellPadding = 8f;
    private const float FuzzyCheckboxWidth = 110f;
    private const float ThumbPad = 4f;
    private static readonly Vector2 DefaultSize = new(1028f, 768f);
    private static readonly Vector2 MinSize = new(640f, 480f);

    private bool _open;

    public void Open(AssetKind kind, Action<string> onPicked, string? currentAssetRelativePath = null)
    {
        thumbnails.Clear();
        session.Open(kind, onPicked, currentAssetRelativePath);
        _open = true;
    }

    public void Render()
    {
        if (!session.IsOpen)
        {
            if (_open)
            {
                _open = false;
                thumbnails.Clear();
            }

            return;
        }

        _open = true;

        ImGui.SetNextWindowSize(DefaultSize, ImGuiCond.Appearing);
        ImGui.SetNextWindowSizeConstraints(MinSize, new Vector2(float.MaxValue, float.MaxValue));

        // No AlwaysAutoResize — user can resize. No NoMove so they can reposition.
        if (!ModalDrawer.BeginCenteredModal(
                "Asset Browser",
                ref _open,
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoCollapse))
        {
            if (!_open)
                Close();
            return;
        }

        if (!session.HasProject || session.Kind is null)
        {
            TextDrawer.DrawErrorText("No project is currently loaded.");
            ImGui.Separator();
            if (ButtonDrawer.DrawModalButton("Close"))
                Close();
            ModalDrawer.EndModal();
            return;
        }

        DrawSearchRow();

        ImGui.BeginChild("##selectAssetGrid", new Vector2(0, -56f), ImGuiChildFlags.Border);
        DrawGroupedGrid();
        ImGui.EndChild();

        if (!string.IsNullOrEmpty(session.StatusMessage))
            TextDrawer.DrawErrorText(session.StatusMessage);

        ImGui.Separator();
        DrawFooterButtons();

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            Close();

        ModalDrawer.EndModal();

        if (!_open)
            Close();
    }

    private void DrawSearchRow()
    {
        var search = session.Search;
        LayoutDrawer.DrawSearchInput(
            "Search files...",
            ref search,
            session.SetSearch,
            trailingReservedWidth: FuzzyCheckboxWidth + ImGui.GetStyle().ItemSpacing.X,
            id: "##selectAssetSearch",
            maxLength: 256);

        ImGui.SameLine();
        var fuzzy = session.Fuzzy;
        if (ImGui.Checkbox("Fuzzy", ref fuzzy))
            session.SetFuzzy(fuzzy);
        LayoutDrawer.DrawTooltip(
            "Fuzzy search matches characters in order, not as an exact phrase.\n" +
            "Example: \"hrfbx\" matches \"assets/models/hero.fbx\".");
    }

    private void DrawFooterButtons()
    {
        if (!FilePicker.IsAvailable)
            return;
        
        var rightX = ImGui.GetWindowContentRegionMax().X - EditorUIConstants.StandardButtonWidth;
        ImGui.SameLine();
        ImGui.SetCursorPosX(rightX);
        if (ButtonDrawer.DrawModalButton("Browse..."))
            session.TryBrowseOs();
    }

    private void DrawGroupedGrid()
    {
        if (session.ShowsImageThumbnails)
            thumbnails.Pump();

        if (session.Filtered.Count == 0)
        {
            ImGui.TextUnformatted(string.IsNullOrEmpty(session.Search)
                ? $"No {session.Kind!.DisplayName.ToLowerInvariant()} assets found in this project."
                : "No matching assets.");
            return;
        }

        var flatIndex = 0;
        foreach (var group in session.Groups)
        {
            ImGui.SeparatorText(group.Folder);
            DrawGroupCells(group.Items, ref flatIndex);
            ImGui.Spacing();
        }
    }

    private void DrawGroupCells(IReadOnlyList<AssetEntry> items, ref int flatIndex)
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var columns = System.Math.Max(1, (int)(avail / (CellSize + CellPadding)));

        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0 && i % columns != 0)
                ImGui.SameLine();

            var entry = items[i];
            var index = flatIndex++;
            var name = Path.GetFileName(entry.DisplayPath);
            var selected = index == session.SelectedIndex;

            ImGui.PushID(entry.AbsolutePath);
            if (selected)
                ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.45f, 0.7f, 0.8f));

            var clicked = session.ShowsImageThumbnails
                ? DrawImageCell(entry.AbsolutePath, name, selected)
                : ImGui.Button(name, new Vector2(CellSize, CellSize));

            if (selected && !session.ShowsImageThumbnails)
                ImGui.PopStyleColor();

            if (clicked)
            {
                session.SelectedIndex = index;
                session.TryConfirm(entry, out _);
            }

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(entry.DisplayPath);

            ImGui.PopID();
        }
    }

    private bool DrawImageCell(string absolutePath, string name, bool selected)
    {
        var thumb = thumbnails.GetOrRequest(absolutePath);
        if (thumb is null)
        {
            var pendingClicked = ImGui.Button(name, new Vector2(CellSize, CellSize));
            if (selected)
                ImGui.PopStyleColor();
            return pendingClicked;
        }

        if (selected)
            ImGui.PopStyleColor();

        var clicked = ImGui.InvisibleButton("##thumb", new Vector2(CellSize, CellSize));
        var min = ImGui.GetItemRectMin();
        var max = ImGui.GetItemRectMax();

        if (selected)
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.2f, 0.45f, 0.7f, 0.8f)));
        else if (ImGui.IsItemHovered())
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
        else if (ImGui.IsItemActive())
            ImGui.GetWindowDrawList().AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.ButtonActive));

        ImGui.GetWindowDrawList().AddImage(
            ImGuiNativeTexture.From(thumb),
            min + new Vector2(ThumbPad, ThumbPad),
            max - new Vector2(ThumbPad, ThumbPad),
            new Vector2(0, 1),
            new Vector2(1, 0));

        return clicked;
    }

    private void Close()
    {
        _open = false;
        session.Close();
        thumbnails.Clear();
    }

    public void Dispose() => thumbnails.Dispose();
}
