using System.Numerics;
using Editor.UI.Constants;
using Editor.Utilities;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Helper class for common ImGui table patterns.
/// </summary>
public static class TableDrawer
{
    /// <summary>
    /// Column definition for table rendering.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>Column header label</summary>
        public required string Label { get; init; }

        /// <summary>Column flags (default: WidthStretch)</summary>
        public ImGuiTableColumnFlags Flags { get; init; } = ImGuiTableColumnFlags.WidthStretch;

        /// <summary>Initial width (used with WidthFixed flag)</summary>
        public float InitWidth { get; init; } = 0;

        /// <summary>Optional custom renderer for this column's cells</summary>
        public Action<int>? CellRenderer { get; init; }
    }

    /// <summary>
    /// Begins a standard table with common flags and column setup.
    /// Must be followed by EndTable() when done.
    /// </summary>
    /// <param name="id">Unique table identifier</param>
    /// <param name="columnCount">Number of columns</param>
    /// <param name="flags">Table flags (default: Borders | RowBg | ScrollY)</param>
    /// <param name="outerSize">Outer size of the table (default: full available space)</param>
    /// <returns>True if the table was successfully created</returns>
    public static bool BeginStandardTable(string id, int columnCount,
        ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
        Vector2? outerSize = null)
    {
        var size = outerSize ?? new Vector2(0, 0);
        return ImGui.BeginTable(id, columnCount, flags, size);
    }

    /// <summary>
    /// Renders a simple two-column table with standard layout.
    /// Commonly used for key-value displays (like shortcuts, properties).
    /// </summary>
    /// <param name="id">Unique table identifier</param>
    /// <param name="leftHeader">Left column header</param>
    /// <param name="rightHeader">Right column header</param>
    /// <param name="leftWidth">Fixed width for left column</param>
    /// <param name="renderRows">Action to render table rows</param>
    public static void DrawTwoColumnTable(string id, string leftHeader, string rightHeader,
        float leftWidth, Action renderRows)
    {
        if (BeginStandardTable(id, 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(leftHeader, ImGuiTableColumnFlags.WidthFixed, leftWidth);
            ImGui.TableSetupColumn(rightHeader, ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            renderRows();

            ImGui.EndTable();
        }
    }

    /// <summary>
    /// Renders a table row with automatic column switching.
    /// </summary>
    /// <param name="cellRenderers">Array of actions to render each cell</param>
    public static void DrawTableRow(params Action[] cellRenderers)
    {
        ImGui.TableNextRow();

        for (var i = 0; i < cellRenderers.Length; i++)
        {
            ImGui.TableSetColumnIndex(i);
            cellRenderers[i]();
        }
    }

    /// <summary>
    /// Renders a colored table cell with text.
    /// </summary>
    /// <param name="text">Text to display</param>
    /// <param name="color">Text color</param>
    public static void DrawColoredCell(string text, Vector4 color)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text(text);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Renders a selectable table row with optional highlighting.
    /// </summary>
    /// <param name="rowId">Unique identifier for the row</param>
    /// <param name="isSelected">Whether this row is selected</param>
    /// <param name="onClicked">Callback when row is clicked</param>
    /// <param name="cellRenderers">Array of actions to render each cell</param>
    /// <returns>True if the row was clicked</returns>
    public static bool DrawSelectableRow(string rowId, bool isSelected,
        Action? onClicked = null, params Action[] cellRenderers)
    {
        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);

        var clicked = ImGui.Selectable($"##{rowId}_selectable", isSelected,
            ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap);

        if (clicked)
            onClicked?.Invoke();

        // Render cells on top of the selectable
        for (var i = 0; i < cellRenderers.Length; i++)
        {
            ImGui.TableSetColumnIndex(i);
            cellRenderers[i]();
        }

        return clicked;
    }

    /// <summary>
    /// Draws an empty table state message.
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="messageType">Type of message (affects color)</param>
    public static void DrawEmptyTableMessage(string message, MessageType messageType = MessageType.Info)
    {
        var color = messageType switch
        {
            MessageType.Error => EditorUIConstants.ErrorColor,
            MessageType.Warning => EditorUIConstants.WarningColor,
            MessageType.Success => EditorUIConstants.SuccessColor,
            MessageType.Info => EditorUIConstants.InfoColor,
            _ => Vector4.One
        };

        TextDrawer.DrawColoredText(message, color);
        // ImGui.PushStyleColor(ImGuiCol.Text, color);
        // ImGui.Text(message);
        // ImGui.PopStyleColor();
    }

    /// <summary>
    /// Renders a generic table with data items.
    /// </summary>
    /// <typeparam name="T">Type of data items</typeparam>
    /// <param name="id">Unique table identifier</param>
    /// <param name="columns">Column definitions</param>
    /// <param name="items">Data items to display</param>
    /// <param name="renderRow">Function to render a single row given an item</param>
    /// <param name="emptyMessage">Message to show when no items (default: "No items to display.")</param>
    /// <param name="flags">Optional table flags</param>
    public static void DrawDataTable<T>(string id, ColumnDefinition[] columns, IEnumerable<T> items,
        Action<T> renderRow, string emptyMessage = "No items to display.",
        ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY)
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            DrawEmptyTableMessage(emptyMessage);
            return;
        }

        if (BeginStandardTable(id, columns.Length, flags))
        {
            SetupColumns(columns);

            foreach (var item in itemList)
            {
                renderRow(item);
            }

            ImGui.EndTable();
        }
    }

    /// <summary>
    /// Renders a sortable table header cell.
    /// </summary>
    /// <param name="label">Header label</param>
    /// <param name="columnId">Column identifier for sorting</param>
    /// <param name="currentSortColumn">Current sort column ID</param>
    /// <param name="isAscending">Whether current sort is ascending</param>
    /// <param name="onSort">Callback when header is clicked (receives column ID and new sort direction)</param>
    public static void DrawSortableHeader(string label, string columnId,
        string currentSortColumn, bool isAscending,
        Action<string, bool> onSort)
    {
        var isCurrentColumn = columnId == currentSortColumn;
        var displayLabel = isCurrentColumn
            ? $"{label} {(isAscending ? "▲" : "▼")}"
            : label;

        if (ImGui.Selectable(displayLabel))
        {
            var newDirection = !isCurrentColumn || !isAscending;
            onSort(columnId, newDirection);
        }
    }

    /// <summary>
    /// Creates a table with sortable headers and selection support.
    /// </summary>
    /// <typeparam name="T">Type of data items</typeparam>
    /// <param name="id">Unique table identifier</param>
    /// <param name="columns">Column definitions</param>
    /// <param name="items">Data items to display</param>
    /// <param name="selectedItem">Currently selected item (can be null)</param>
    /// <param name="getItemId">Function to get unique ID for an item</param>
    /// <param name="onItemSelected">Callback when an item is selected</param>
    /// <param name="renderRow">Function to render row cells for an item</param>
    /// <param name="emptyMessage">Message to show when no items</param>
    public static void DrawSelectableDataTable<T>(string id, ColumnDefinition[] columns,
        IEnumerable<T> items, T? selectedItem,
        Func<T, string> getItemId, Action<T> onItemSelected,
        Action<T> renderRow, string emptyMessage = "No items to display.") where T : class
    {
        var itemList = items.ToList();

        if (itemList.Count == 0)
        {
            DrawEmptyTableMessage(emptyMessage);
            return;
        }

        if (BeginStandardTable(id, columns.Length))
        {
            SetupColumns(columns);

            foreach (var item in itemList)
            {
                ImGui.TableNextRow();

                var itemId = getItemId(item);
                var isSelected = selectedItem != null && getItemId(selectedItem) == itemId;

                ImGui.TableSetColumnIndex(0);
                if (ImGui.Selectable($"##{itemId}", isSelected,
                    ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap))
                {
                    onItemSelected(item);
                }

                // Render cells on top of selectable
                for (var i = 0; i < columns.Length; i++)
                {
                    ImGui.TableSetColumnIndex(i);
                    columns[i].CellRenderer?.Invoke(itemList.IndexOf(item));
                }

                renderRow(item);
            }

            ImGui.EndTable();
        }
    }
    
    /// <summary>
    /// Sets up table columns from an array of column definitions.
    /// </summary>
    /// <param name="columns">Array of column definitions</param>
    private static void SetupColumns(params ColumnDefinition[] columns)
    {
        foreach (var column in columns)
        {
            ImGui.TableSetupColumn(column.Label, column.Flags, column.InitWidth);
        }
        ImGui.TableHeadersRow();
    }
}
