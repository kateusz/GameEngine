# EditorUIConstants Reference

Complete documentation for all constants in `Editor.UI.Constants.EditorUIConstants`.

**Location**: `Editor/UI/Constants/EditorUIConstants.cs`

---

## Button Sizes

### StandardButtonWidth
```csharp
public const float StandardButtonWidth = 120f;
```
Standard width for modal buttons (Create, Save, Cancel, etc.)

**Usage**: Default width for ButtonDrawer.DrawButton(), ButtonDrawer.DrawModalButton()

### StandardButtonHeight
```csharp
public const float StandardButtonHeight = 0f;
```
Height for standard buttons. **0 = auto-height based on content**

**Usage**: Default height for all ButtonDrawer methods

### WideButtonWidth
```csharp
public const float WideButtonWidth = 150f;
```
Width for wide action buttons (Add Existing Script, Create New Script, etc.)

**Usage**: Wide buttons that need more space than standard buttons

### SmallButtonSize
```csharp
public const float SmallButtonSize = 20f;
```
Width/height for small square buttons (axis labels, remove component, etc.)

**Usage**: ButtonDrawer.DrawSmallButton(), compact controls like "X", "Y", "Z" axis buttons

### MediumButtonWidth
```csharp
public const float MediumButtonWidth = 100f;
```
Width for medium-sized buttons (Load OBJ, etc.)

**Usage**: Between standard and small buttons

### IconSize
```csharp
public const float IconSize = 16f;
```
Standard size for icon buttons

**Usage**: Icon dimensions in toolbar and panels

---

## Layout Ratios

### PropertyLabelRatio
```csharp
public const float PropertyLabelRatio = 0.33f;
```
Ratio for property label column (1/3 of available width)

**Usage**: VectorPanel and other property editors for consistent label sizing

### PropertyInputRatio
```csharp
public const float PropertyInputRatio = 0.67f;
```
Ratio for property input column (2/3 of available width)

**Usage**: Complementary to PropertyLabelRatio for input controls

**Example**:
```csharp
var totalWidth = ImGui.GetContentRegionAvail().X;
var labelWidth = totalWidth * EditorUIConstants.PropertyLabelRatio;
var inputWidth = totalWidth * EditorUIConstants.PropertyInputRatio;
```

---

## Column Widths

### DefaultColumnWidth
```csharp
public const float DefaultColumnWidth = 60f;
```
Default width for narrow label columns (e.g., "Tag" label in EntityNameEditor)

**Usage**: Short labels in two-column layouts

### WideColumnWidth
```csharp
public const float WideColumnWidth = 120f;
```
Width for wider columns that need more space

**Usage**: Longer labels or combo boxes

### FilterInputWidth
```csharp
public const float FilterInputWidth = 200f;
```
Width for filter input controls in panels

**Usage**: LayoutDrawer.DrawFilterInput() default width

---

## Spacing and Padding

### StandardPadding
```csharp
public const float StandardPadding = 4f;
```
Standard padding for frame elements

**Usage**: ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(StandardPadding, StandardPadding))

### LargePadding
```csharp
public const float LargePadding = 8f;
```
Larger padding for elements that need more spacing

**Usage**: Section separators, grouped elements

### SmallPadding
```csharp
public const float SmallPadding = 2f;
```
Small padding for compact layouts

**Usage**: Tight layouts, toolbar spacing

---

## Input Buffer Limits

### MaxTextInputLength
```csharp
public const uint MaxTextInputLength = 256;
```
Maximum length for general text input fields (names, labels, etc.)

**Usage**: ImGui.InputText buffer size for standard inputs

### MaxPathLength
```csharp
public const uint MaxPathLength = 512;
```
Maximum length for file paths and directory paths

**Usage**: Path input buffers

### MaxNameLength
```csharp
public const uint MaxNameLength = 100;
```
Maximum length for short names (script names, project names, etc.)

**Usage**: More constrained input fields, LayoutDrawer.DrawSearchInput()

---

## Standard Colors

### ErrorColor
```csharp
public static readonly Vector4 ErrorColor = new(1.0f, 0.3f, 0.3f, 1.0f);
```
Error color for validation messages and error indicators (**bright red**)

**RGB**: (255, 77, 77)
**Usage**: TextDrawer.DrawErrorText(), ButtonDrawer.DrawColoredButton(MessageType.Error)

### WarningColor
```csharp
public static readonly Vector4 WarningColor = new(1.0f, 1.0f, 0.0f, 1.0f);
```
Warning color for warning messages and caution indicators (**bright yellow**)

**RGB**: (255, 255, 0)
**Usage**: TextDrawer.DrawWarningText(), ButtonDrawer.DrawColoredButton(MessageType.Warning)

### SuccessColor
```csharp
public static readonly Vector4 SuccessColor = new(0.3f, 1.0f, 0.3f, 1.0f);
```
Success color for positive feedback and confirmation messages (**bright green**)

**RGB**: (77, 255, 77)
**Usage**: TextDrawer.DrawSuccessText(), ButtonDrawer.DrawColoredButton(MessageType.Success)

### InfoColor
```csharp
public static readonly Vector4 InfoColor = new(0.7f, 0.7f, 0.7f, 1.0f);
```
Info/neutral text color (**light gray**)

**RGB**: (179, 179, 179)
**Usage**: TextDrawer.DrawInfoText(), neutral messages

---

## Axis Colors

Used for transform gizmos and vector editors (X, Y, Z)

### AxisXColor
```csharp
public static readonly Vector4 AxisXColor = new(0.8f, 0.1f, 0.15f, 1.0f);
```
X-axis color (**red**)

**RGB**: (204, 26, 38)
**Usage**: Vector X component in VectorPanel

### AxisYColor
```csharp
public static readonly Vector4 AxisYColor = new(0.2f, 0.7f, 0.2f, 1.0f);
```
Y-axis color (**green**)

**RGB**: (51, 179, 51)
**Usage**: Vector Y component in VectorPanel

### AxisZColor
```csharp
public static readonly Vector4 AxisZColor = new(0.1f, 0.25f, 0.8f, 1.0f);
```
Z-axis color (**blue**)

**RGB**: (26, 64, 204)
**Usage**: Vector Z component in VectorPanel

**Example**:
```csharp
ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisXColor);
ImGui.Button("X");
ImGui.PopStyleColor();
```

---

## Common UI Sizes

### SelectorListBoxWidth
```csharp
public const float SelectorListBoxWidth = 300f;
```
Width for list boxes in selector popups (e.g., script selector)

**Usage**: ModalDrawer.RenderListSelectionModal()

### MaxVisibleListItems
```csharp
public const int MaxVisibleListItems = 10;
```
Maximum number of visible items in list boxes before scrolling

**Usage**: Calculating listbox height in ModalDrawer

**Example**:
```csharp
var itemHeight = ImGui.GetTextLineHeightWithSpacing();
var visibleItems = Math.Min(items.Length, EditorUIConstants.MaxVisibleListItems);
var listboxHeight = itemHeight * visibleItems;
```

---

## Usage Guidelines

### When to Use Constants

✅ **Always use constants for**:
- Button dimensions
- Padding and spacing
- Semantic colors (Error, Warning, Success, Info)
- Axis colors
- Input buffer sizes
- Common widths (columns, filters)

❌ **Don't use constants for**:
- Dynamic sizes based on content
- One-off custom layouts
- Proportional calculations (use ratios instead)

### Example: Property Editor Layout
```csharp
var totalWidth = ImGui.GetContentRegionAvail().X;
var labelWidth = totalWidth * EditorUIConstants.PropertyLabelRatio;
var inputWidth = totalWidth * EditorUIConstants.PropertyInputRatio;

ImGui.Text("Position");
ImGui.SameLine(labelWidth);
ImGui.SetNextItemWidth(inputWidth);
ImGui.DragFloat("##X", ref x);
```

### Example: Semantic Color Usage
```csharp
// ✅ CORRECT - Use semantic constants
if (hasError)
    TextDrawer.DrawErrorText("Validation failed");
else
    TextDrawer.DrawSuccessText("Validation passed");

// ❌ WRONG - Don't hardcode colors
if (hasError)
    TextDrawer.DrawColoredText("Validation failed", new Vector4(1, 0, 0, 1));
```

---

## Summary

**Total Constants**: 22

**Categories**:
- **Button Sizes** (6): StandardButtonWidth, StandardButtonHeight, WideButtonWidth, SmallButtonSize, MediumButtonWidth, IconSize
- **Layout Ratios** (2): PropertyLabelRatio, PropertyInputRatio
- **Column Widths** (3): DefaultColumnWidth, WideColumnWidth, FilterInputWidth
- **Spacing** (3): StandardPadding, LargePadding, SmallPadding
- **Input Limits** (3): MaxTextInputLength, MaxPathLength, MaxNameLength
- **Standard Colors** (4): ErrorColor, WarningColor, SuccessColor, InfoColor
- **Axis Colors** (3): AxisXColor, AxisYColor, AxisZColor
- **UI Sizes** (2): SelectorListBoxWidth, MaxVisibleListItems

**Key Philosophy**: Centralize all magic numbers to ensure consistency and make global styling adjustments easy.