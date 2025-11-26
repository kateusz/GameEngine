using System.Numerics;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Helper class for common ImGui tree node and hierarchy patterns.
/// </summary>
public static class TreeDrawer
{
    /// <summary>
    /// Draws a selectable tree node with optional context menu and callbacks.
    /// </summary>
    /// <param name="label">Node label to display</param>
    /// <param name="isSelected">Whether this node is currently selected</param>
    /// <param name="onClicked">Callback when node is clicked</param>
    /// <param name="onContextMenu">Optional callback to render context menu items</param>
    /// <param name="flags">Additional tree node flags</param>
    /// <returns>True if the tree node is open (expanded)</returns>
    public static bool DrawSelectableTreeNode(string label, bool isSelected,
        Action? onClicked = null,
        Action? onContextMenu = null,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        var nodeFlags = flags | ImGuiTreeNodeFlags.OpenOnArrow;
        if (isSelected)
            nodeFlags |= ImGuiTreeNodeFlags.Selected;

        var opened = ImGui.TreeNodeEx(label, nodeFlags, label);

        if (ImGui.IsItemClicked())
        {
            onClicked?.Invoke();
        }

        if (onContextMenu != null && ImGui.BeginPopupContextItem())
        {
            onContextMenu();
            ImGui.EndPopup();
        }

        return opened;
    }
    
    /// <summary>
    /// Draws a selectable item (non-tree) with optional context menu.
    /// Useful for flat list views with context menu support.
    /// </summary>
    /// <param name="label">Item label to display</param>
    /// <param name="isSelected">Whether this item is currently selected</param>
    /// <param name="onClicked">Callback when item is clicked</param>
    /// <param name="onContextMenu">Optional callback to render context menu items</param>
    /// <param name="onDoubleClick">Optional callback when item is double-clicked</param>
    /// <returns>True if the item was clicked</returns>
    public static bool DrawSelectableItem(string label, bool isSelected,
        Action? onClicked = null,
        Action? onContextMenu = null,
        Action? onDoubleClick = null)
    {
        var clicked = ImGui.Selectable(label, isSelected);
        if (clicked) 
            onClicked?.Invoke();

        if (onDoubleClick != null && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
            onDoubleClick();

        if (onContextMenu != null && ImGui.BeginPopupContextItem())
        {
            onContextMenu();
            ImGui.EndPopup();
        }

        return clicked;
    }

    /// <summary>
    /// Draws a tree node with colored text for highlighting.
    /// Useful for search results or special states.
    /// </summary>
    /// <param name="label">Node label to display</param>
    /// <param name="color">Text color</param>
    /// <param name="isSelected">Whether this node is currently selected</param>
    /// <param name="onClicked">Callback when node is clicked</param>
    /// <param name="onContextMenu">Optional callback to render context menu items</param>
    /// <param name="flags">Additional tree node flags</param>
    /// <returns>True if the tree node is open (expanded)</returns>
    public static bool DrawColoredTreeNode(string label, Vector4 color, bool isSelected,
        Action? onClicked = null,
        Action? onContextMenu = null,
        ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
    {
        var opened = false;

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        opened = DrawSelectableTreeNode(label, isSelected, onClicked, onContextMenu, flags);
        ImGui.PopStyleColor();

        return opened;
    }
    
    /// <summary>
    /// Renders a hierarchical tree structure recursively.
    /// </summary>
    /// <typeparam name="T">Type of tree node data</typeparam>
    /// <param name="node">Current node to render</param>
    /// <param name="getChildren">Function to get children of a node</param>
    /// <param name="getLabel">Function to get label for a node</param>
    /// <param name="isSelected">Function to check if a node is selected</param>
    /// <param name="onClicked">Callback when a node is clicked</param>
    /// <param name="onContextMenu">Optional callback to render context menu for a node</param>
    public static void DrawHierarchy<T>(T node,
        Func<T, IEnumerable<T>> getChildren,
        Func<T, string> getLabel,
        Func<T, bool> isSelected,
        Action<T>? onClicked = null,
        Action<T>? onContextMenu = null)
    {
        var label = getLabel(node);
        var selected = isSelected(node);
        var children = getChildren(node).ToList();

        var flags = children.Count == 0
            ? ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen
            : ImGuiTreeNodeFlags.None;

        var opened = DrawSelectableTreeNode(label, selected,
            onClicked: () => onClicked?.Invoke(node),
            onContextMenu: onContextMenu != null ? () => onContextMenu(node) : null,
            flags: flags);

        if (opened && children.Count > 0)
        {
            foreach (var child in children)
            {
                DrawHierarchy(child, getChildren, getLabel, isSelected, onClicked, onContextMenu);
            }
            ImGui.TreePop();
        }
    }
}
