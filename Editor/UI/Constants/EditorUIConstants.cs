using System.Numerics;

namespace Editor.UI.Constants;

/// <summary>
/// Standard UI dimensions and styling constants for the Editor.
/// Centralizes all magic numbers used throughout the editor UI to ensure consistency
/// and make it easier to adjust styling globally.
/// </summary>
public static class EditorUIConstants
{
    /// <summary>
    /// Standard width for modal buttons (Create, Save, Cancel, etc.)
    /// </summary>
    public const float StandardButtonWidth = 120f;

    /// <summary>
    /// Tooltip for asset Open / drag-drop field buttons.
    /// </summary>
    public const string SelectAssetTooltip = "Select Asset or search on computer";
    
    /// <summary>
    /// Height for standard buttons (0 = auto-height based on content)
    /// </summary>
    public const float StandardButtonHeight = 0f;
    
    /// <summary>
    /// Width/height for small square buttons (axis labels, remove component, etc.)
    /// Used for compact controls like "X", "Y", "Z" axis buttons
    /// </summary>
    public const float SmallButtonSize = 20f;
    
    /// <summary>
    /// Ratio for property label column (1/3 of available width)
    /// Used in VectorPanel and other property editors for consistent label sizing
    /// </summary>
    public const float PropertyLabelRatio = 0.33f;
    
    /// <summary>
    /// Ratio for property input column (2/3 of available width)
    /// Complementary to PropertyLabelRatio for input controls
    /// </summary>
    public const float PropertyInputRatio = 0.67f;
    
    /// <summary>
    /// Default width for narrow label columns (e.g., "Tag" label in EntityNameEditor)
    /// </summary>
    public const float DefaultColumnWidth = 60f;
    
    /// <summary>
    /// Width for wider columns that need more space
    /// </summary>
    public const float WideColumnWidth = 120f;
    
    /// <summary>
    /// Width for filter input controls in panels
    /// </summary>
    public const float FilterInputWidth = 200f;
    
    /// <summary>
    /// Standard padding for frame elements (ImGui.PushStyleVar FramePadding)
    /// </summary>
    public const float StandardPadding = 4f;
    
    /// <summary>
    /// Small padding for compact layouts
    /// </summary>
    public const float SmallPadding = 2f;
    
    /// <summary>
    /// Maximum length for general text input fields (names, labels, etc.)
    /// Used with ImGui.InputText for buffers
    /// </summary>
    public const uint MaxTextInputLength = 256;
    
    /// <summary>
    /// Maximum length for file paths and directory paths
    /// </summary>
    public const uint MaxPathLength = 512;
    
    /// <summary>
    /// Maximum length for short names (script names, project names, etc.)
    /// Used for more constrained input fields
    /// </summary>
    public const uint MaxNameLength = 100;
    
    /// <summary>
    /// Error color for validation messages and error indicators (bright red)
    /// </summary>
    public static readonly Vector4 ErrorColor = new(1.0f, 0.3f, 0.3f, 1.0f);
    
    /// <summary>
    /// Warning color for warning messages and caution indicators (bright yellow)
    /// </summary>
    public static readonly Vector4 WarningColor = new(1.0f, 1.0f, 0.0f, 1.0f);
    
    /// <summary>
    /// Success color for positive feedback and confirmation messages (bright green)
    /// </summary>
    public static readonly Vector4 SuccessColor = new(0.3f, 1.0f, 0.3f, 1.0f);
    
    /// <summary>
    /// Info/neutral text color (light gray)
    /// </summary>
    public static readonly Vector4 InfoColor = new(0.7f, 0.7f, 0.7f, 1.0f);

    /// <summary>
    /// Scene hierarchy row background when the entity is in the current selection.
    /// </summary>
    public static readonly Vector4 HierarchyRowSelectedBackground = new(0.20f, 0.45f, 0.82f, 0.72f);

    /// <summary>
    /// Scene hierarchy row label when selected (high contrast on <see cref="HierarchyRowSelectedBackground"/>).
    /// </summary>
    public static readonly Vector4 HierarchyRowSelectedText = new(1.0f, 1.0f, 1.0f, 1.0f);

    /// <summary>
    /// Left-edge accent on selected hierarchy rows.
    /// </summary>
    public static readonly Vector4 HierarchyRowSelectedAccent = new(0.45f, 0.75f, 1.0f, 1.0f);
    
    /// <summary>
    /// X-axis color (red) - used for transform gizmos and vector editors
    /// </summary>
    public static readonly Vector4 AxisXColor = new(0.8f, 0.1f, 0.15f, 1.0f);
    
    /// <summary>
    /// Y-axis color (green) - used for transform gizmos and vector editors
    /// </summary>
    public static readonly Vector4 AxisYColor = new(0.2f, 0.7f, 0.2f, 1.0f);
    
    /// <summary>
    /// Z-axis color (blue) - used for transform gizmos and vector editors
    /// </summary>
    public static readonly Vector4 AxisZColor = new(0.1f, 0.25f, 0.8f, 1.0f);
    
    /// <summary>
    /// Width for list boxes in selector popups (e.g., script selector)
    /// </summary>
    public const float SelectorListBoxWidth = 300f;
    
    /// <summary>
    /// Maximum number of visible items in list boxes before scrolling
    /// </summary>
    public const int MaxVisibleListItems = 10;

    /// <summary>
    /// Width for toolbar toggle buttons (grid, ruler, etc.)
    /// </summary>
    public const float ToolbarToggleWidth = 25f;

    /// <summary>
    /// Height for toolbar toggle buttons (grid, ruler, etc.)
    /// </summary>
    public const float ToolbarToggleHeight = 19f;
    
    /// <summary>
    /// Label column width for form-style dialogs (Export, Project Settings, New Project, …).
    /// </summary>
    public const float ModalLabelWidth = 130f;

    /// <summary>
    /// Minimum width for form-style dialogs.
    /// </summary>
    public const float ModalMinWidth = 520f;

    // Viewport grid / ruler
    public const float GridMinorLineOpacity = 0.30f;
    public const float GridMajorLineOpacity = 0.55f;
    public const int GridMajorStep = 10;
    public const float TargetPixelSpacing = 50.0f;
}
