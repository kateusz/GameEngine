using System.Numerics;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using ImGuiNET;

namespace Editor.Commands;

public class CommandPalette(CommandRegistry registry)
{
    private const string PopupId = "Command Palette";
    private const float RowPadX = 6f;
    private const float ShortcutPadX = 8f;

    private bool _open;
    private bool _focusFilter;
    private string _filter = string.Empty;
    private int _highlight;
    private IReadOnlyList<CommandPaletteItem> _visible = [];

    public void Show()
    {
        _open = true;
        _filter = string.Empty;
        _highlight = 0;
        _focusFilter = true;
    }

    public void Render()
    {
        if (!_open)
            return;

        ImGui.SetNextWindowSize(new Vector2(520, 360), ImGuiCond.Appearing);
        if (!ModalDrawer.BeginCenteredModal(PopupId, ref _open, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize))
            return;

        if (_focusFilter)
        {
            ImGui.SetKeyboardFocusHere();
            _focusFilter = false;
        }

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint("##cmdFilter", "Type a command...", ref _filter, EditorUIConstants.MaxTextInputLength))
            _highlight = 0;

        RefreshVisible();

        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && _visible.Count > 0)
                _highlight = (_highlight - 1 + _visible.Count) % _visible.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.DownArrow) && _visible.Count > 0)
                _highlight = (_highlight + 1) % _visible.Count;
            if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
                TryRunHighlighted();
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
                Close();
        }

        ImGui.BeginChild("##cmdList", new Vector2(0, 280), ImGuiChildFlags.None);
        var drawList = ImGui.GetWindowDrawList();
        var rowHeight = ImGui.GetTextLineHeightWithSpacing();
        var textHeight = ImGui.GetTextLineHeight();

        for (var i = 0; i < _visible.Count; i++)
        {
            var item = _visible[i];
            var selected = i == _highlight;
            var width = ImGui.GetContentRegionAvail().X;
            var rowMin = ImGui.GetCursorScreenPos();
            var rowMax = new Vector2(rowMin.X + width, rowMin.Y + rowHeight);

            if (selected)
            {
                drawList.AddRectFilled(rowMin, rowMax,
                    ImGui.ColorConvertFloat4ToU32(EditorUIConstants.HierarchyRowSelectedBackground));
                drawList.AddRectFilled(rowMin, new Vector2(rowMin.X + 3f, rowMax.Y),
                    ImGui.ColorConvertFloat4ToU32(EditorUIConstants.HierarchyRowSelectedAccent));
            }

            if (ImGui.InvisibleButton($"##{item.Id}", new Vector2(width, rowHeight)))
            {
                _highlight = i;
                TryRunHighlighted();
            }

            if (ImGui.IsItemHovered())
                _highlight = i;

            var muted = selected || !item.Enabled
                ? (selected
                    ? EditorUIConstants.HierarchyRowSelectedText
                    : EditorUIConstants.InfoColor)
                : EditorUIConstants.InfoColor;
            var titleColor = selected
                ? EditorUIConstants.HierarchyRowSelectedText
                : item.Enabled
                    ? new Vector4(1f, 1f, 1f, 1f)
                    : EditorUIConstants.InfoColor;

            var textY = rowMin.Y + (rowHeight - textHeight) * 0.5f;
            var prefix = $"{item.Category}: ";
            var prefixSize = ImGui.CalcTextSize(prefix);
            drawList.AddText(new Vector2(rowMin.X + RowPadX, textY),
                ImGui.ColorConvertFloat4ToU32(muted), prefix);
            drawList.AddText(new Vector2(rowMin.X + RowPadX + prefixSize.X, textY),
                ImGui.ColorConvertFloat4ToU32(titleColor), item.Title);

            if (item.ShortcutDisplay is { } chord)
            {
                var chordSize = ImGui.CalcTextSize(chord);
                drawList.AddText(
                    new Vector2(rowMax.X - chordSize.X - ShortcutPadX, textY),
                    ImGui.ColorConvertFloat4ToU32(muted),
                    chord);
            }
        }

        ImGui.EndChild();

        if (_visible.Count == 0)
            ImGui.TextDisabled("No matching commands");
        else if (!_visible[_highlight].Enabled && !string.IsNullOrEmpty(_visible[_highlight].DisabledReason))
            ImGui.TextDisabled(_visible[_highlight].DisabledReason);

        ModalDrawer.EndModal();
    }

    private void RefreshVisible()
    {
        var working = registry.GetWorkingSet()
            .OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Title, StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_filter))
        {
            _visible = working.ToList();
        }
        else
        {
            _visible = working
                .Where(c =>
                    c.Title.Contains(_filter, StringComparison.OrdinalIgnoreCase)
                    || c.Category.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (_visible.Count == 0)
            _highlight = 0;
        else if (_highlight >= _visible.Count)
            _highlight = _visible.Count - 1;
    }

    private void TryRunHighlighted()
    {
        if (_visible.Count == 0)
            return;

        var item = _visible[_highlight];
        if (!item.Enabled)
            return;

        registry.Execute(item.Id);
        Close();
    }

    private void Close()
    {
        _open = false;
        ImGui.CloseCurrentPopup();
    }
}
