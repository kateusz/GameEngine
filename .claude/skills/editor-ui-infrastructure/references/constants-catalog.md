# EditorUIConstants Catalog

Complete reference for all Editor UI constants. These constants ensure visual consistency across the editor.

## Table of Contents
1. [Button Sizes](#button-sizes)
2. [Layout Ratios](#layout-ratios)
3. [Column Widths](#column-widths)
4. [Spacing](#spacing)
5. [Input Buffers](#input-buffers)
6. [Colors](#colors)
7. [UI Sizes](#ui-sizes)
8. [Design Rationale](#design-rationale)

---

## Button Sizes

Standard button dimensions used throughout the editor.

| Constant | Value | Usage |
|----------|-------|-------|
| `StandardButtonWidth` | 120 | Default button width for most actions |
| `StandardButtonHeight` | 30 | Default button height for all buttons |
| `WideButtonWidth` | 200 | Wide buttons (export, settings, primary actions) |
| `MediumButtonWidth` | 100 | Medium buttons (OK, Cancel in compact spaces) |
| `SmallButtonSize` | 20 | Small square buttons (×, +, - icons) |

### Usage Example

```csharp
// Standard button
ButtonDrawer.DrawButton("Save",
    width: EditorUIConstants.StandardButtonWidth,
    height: EditorUIConstants.StandardButtonHeight
);

// Wide button
ButtonDrawer.DrawButton("Export Project",
    width: EditorUIConstants.WideButtonWidth,
    height: EditorUIConstants.StandardButtonHeight
);

// Small button
ButtonDrawer.DrawSmallButton("×"); // Uses SmallButtonSize (20×20)
```

### Design Note

Button heights are consistent (30px) to maintain visual rhythm. Widths vary based on action importance and label length.

---

## Layout Ratios

Proportions for component editor layouts (property label vs. input field).

| Constant | Value | Usage |
|----------|-------|-------|
| `PropertyLabelRatio` | 0.33f | Label width as fraction of total (33%) |
| `PropertyInputRatio` | 0.67f | Input width as fraction of total (67%) |

### Usage Example

```csharp
public void DrawProperty()
{
    float totalWidth = ImGui.GetContentRegionAvail().X;
    float labelWidth = totalWidth * EditorUIConstants.PropertyLabelRatio;
    float inputWidth = totalWidth * EditorUIConstants.PropertyInputRatio;

    ImGui.Text("Speed");
    ImGui.SameLine(labelWidth);
    ImGui.SetNextItemWidth(inputWidth);
    ImGui.DragFloat("##speed", ref speed);
}
```

### Visual Layout

```
┌─────────────────────────────────────────┐
│ Label (33%)    │  Input Field (67%)     │
└─────────────────────────────────────────┘
```

### Design Note

The 33/67 split provides balance:
- Labels have enough space to be readable without truncation
- Input fields have generous space for content (vectors, long strings)
- Consistent with industry standards (Unity, Unreal)
- Tested across various property types

---

## Column Widths

Standard column widths for tables and lists.

| Constant | Value | Usage |
|----------|-------|-------|
| `DefaultColumnWidth` | 150 | Standard column width for tables |
| `WideColumnWidth` | 300 | Wide columns (file paths, descriptions) |
| `FilterInputWidth` | 200 | Search/filter input fields |

### Usage Example

```csharp
// Table with default columns
ImGui.SetNextItemWidth(EditorUIConstants.DefaultColumnWidth);
ImGui.TableSetupColumn("Name");

// Wide column for paths
ImGui.SetNextItemWidth(EditorUIConstants.WideColumnWidth);
ImGui.TableSetupColumn("Path");

// Filter input
ImGui.SetNextItemWidth(EditorUIConstants.FilterInputWidth);
ImGui.InputText("##filter", buffer, bufferSize);
```

---

## Spacing

Vertical and horizontal spacing values.

| Constant | Value | Usage |
|----------|-------|-------|
| `StandardPadding` | 8 | Default spacing between UI elements |
| `LargePadding` | 16 | Spacing between major sections |
| `SmallPadding` | 4 | Compact spacing (list items, grouped controls) |

### Usage Example

```csharp
// Standard spacing (most common)
LayoutDrawer.DrawSpacing(); // Uses StandardPadding (8px)

// Section separator with large spacing
LayoutDrawer.DrawSpacing(EditorUIConstants.LargePadding);
LayoutDrawer.DrawSeparator();
LayoutDrawer.DrawSpacing(EditorUIConstants.LargePadding);

// Compact list with small spacing
foreach (var item in items)
{
    ImGui.Text(item.Name);
    LayoutDrawer.DrawSpacing(EditorUIConstants.SmallPadding);
}

// Custom padding for style variables
ImGui.PushStyleVar(ImGuiStyleVar.FramePadding,
    new Vector2(EditorUIConstants.StandardPadding, EditorUIConstants.StandardPadding));
```

### Visual Spacing Guide

```
┌────────────────────┐
│ Element            │
│                    │ ← StandardPadding (8px)
│ Element            │
│                    │ ← LargePadding (16px)
│ ═══════════════════│ ← Section Separator
│                    │ ← LargePadding (16px)
│ Element            │
│                    │ ← SmallPadding (4px)
│ Element            │
└────────────────────┘
```

### Design Note

- **StandardPadding (8px)**: Matches typical ImGui spacing, comfortable for most layouts
- **LargePadding (16px)**: Creates clear visual separation between sections
- **SmallPadding (4px)**: Keeps related items visually grouped without wasted space

---

## Input Buffers

Buffer sizes for ImGui text inputs.

| Constant | Value | Usage |
|----------|-------|-------|
| `MaxTextInputLength` | 256 | General text input (descriptions, labels) |
| `MaxNameLength` | 128 | Entity names, component names, short identifiers |
| `MaxPathLength` | 512 | File paths, URLs, long strings |

### Usage Example

```csharp
public class MyPanel
{
    // Declare buffers using constants
    private readonly byte[] _nameBuffer = new byte[EditorUIConstants.MaxNameLength];
    private readonly byte[] _pathBuffer = new byte[EditorUIConstants.MaxPathLength];
    private readonly byte[] _descBuffer = new byte[EditorUIConstants.MaxTextInputLength];

    public void OnImGuiRender()
    {
        // Use buffers with ImGui
        ImGui.InputText("Name", _nameBuffer, (uint)_nameBuffer.Length);
        ImGui.InputText("Path", _pathBuffer, (uint)_pathBuffer.Length);
        ImGui.InputTextMultiline("Description", _descBuffer, (uint)_descBuffer.Length);
    }
}
```

### Design Note

Buffer sizes chosen to balance memory usage and typical content length:
- **Names**: Rarely exceed 128 characters
- **Paths**: 512 supports deep directory structures
- **Text**: 256 is sufficient for descriptions without excessive allocation

---

## Colors

Semantic color constants for UI feedback.

### Message Colors

| Constant | RGB Value | Usage |
|----------|-----------|-------|
| `ErrorColor` | (1.0, 0.0, 0.0, 1.0) | Error messages, destructive actions |
| `WarningColor` | (1.0, 1.0, 0.0, 1.0) | Warning messages, cautions |
| `SuccessColor` | (0.0, 1.0, 0.0, 1.0) | Success messages, confirmations |
| `InfoColor` | (0.0, 0.5, 1.0, 1.0) | Informational messages, help text |

### Axis Colors (Vector Editors)

| Constant | RGB Value | Usage |
|----------|-----------|-------|
| `AxisXColor` | (0.8, 0.2, 0.2, 1.0) | X-axis (Red) for Vector2/Vector3 editors |
| `AxisYColor` | (0.2, 0.8, 0.2, 1.0) | Y-axis (Green) for Vector2/Vector3 editors |
| `AxisZColor` | (0.2, 0.2, 0.8, 1.0) | Z-axis (Blue) for Vector3 editors |

### Usage Example

```csharp
// Message colors
TextDrawer.DrawText("File saved successfully!", MessageType.Success);
// Uses EditorUIConstants.SuccessColor

ButtonDrawer.DrawColoredButton("Delete", MessageType.Error);
// Uses EditorUIConstants.ErrorColor

// Axis colors (automatic in Vector3FieldEditor)
ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisXColor);
ImGui.DragFloat("X", ref vector.X);
ImGui.PopStyleColor();

ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisYColor);
ImGui.DragFloat("Y", ref vector.Y);
ImGui.PopStyleColor();

ImGui.PushStyleColor(ImGuiCol.Button, EditorUIConstants.AxisZColor);
ImGui.DragFloat("Z", ref vector.Z);
ImGui.PopStyleColor();
```

### Color Visual Guide

```
┌─────────────────────────────────────────┐
│ ✓ Success!        (Green - 0,255,0)     │
│ ⚠ Warning         (Yellow - 255,255,0)  │
│ ✕ Error           (Red - 255,0,0)       │
│ ℹ Info            (Blue - 0,128,255)    │
└─────────────────────────────────────────┘

Vector3 Editor:
┌─────────────────────────────────────────┐
│ X [■■■] 1.0    (Red button)             │
│ Y [■■■] 2.0    (Green button)           │
│ Z [■■■] 3.0    (Blue button)            │
└─────────────────────────────────────────┘
```

### Design Note

**Message Colors**:
- Pure colors for maximum visibility and clarity
- Follows universal color conventions (red=danger, green=success)
- High contrast against typical dark editor themes

**Axis Colors**:
- Standard 3D convention (X=red, Y=green, Z=blue)
- Slightly desaturated (0.8 vs 1.0) for softer appearance
- Consistent with Blender, Unity, Unreal axis coloring

---

## UI Sizes

Miscellaneous UI element sizes.

| Constant | Value | Usage |
|----------|-------|-------|
| `SelectorListBoxWidth` | 300 | Width of list boxes in selectors |
| `MaxVisibleListItems` | 10 | Max items shown before scrolling |

### Usage Example

```csharp
// Component selector listbox
ImGui.SetNextItemWidth(EditorUIConstants.SelectorListBoxWidth);
if (ImGui.BeginListBox("##components"))
{
    for (int i = 0; i < Math.Min(components.Count, EditorUIConstants.MaxVisibleListItems); i++)
    {
        ImGui.Selectable(components[i].Name);
    }
    ImGui.EndListBox();
}
```

### Design Note

- **SelectorListBoxWidth (300)**: Wide enough for component names without horizontal scrolling
- **MaxVisibleListItems (10)**: Shows enough items without overwhelming UI, encourages use of search/filter

---

## Design Rationale

### Why These Values?

#### PropertyLabelRatio (0.33f)
**Problem**: Need to balance label readability with input field space.

**Solution**: 33/67 split
- Labels have ~100-150px in typical panels (300-450px wide)
- Sufficient for most property names without truncation
- Input fields get majority of space for vectors, strings, enums
- Tested across all 18 component types in the engine

**Alternatives Considered**:
- 40/60: Labels too wide, wasted space on short names
- 25/75: Labels too narrow, frequent truncation ("Transform...", "Collision...")

#### StandardButtonHeight (30px)
**Problem**: Buttons must be easy to click (Fitts's Law) while maintaining density.

**Solution**: 30px height
- Large enough for comfortable mouse targeting
- Matches ImGui default (FramePadding×2 + FontSize)
- Scales well across font sizes (12-16pt typical in editors)
- Consistent with industry standards (Unity ~28px, Unreal ~32px)

**Supporting Math**:
- Fitts's Law: Time = a + b × log₂(D/W + 1)
- 30px height provides good W (width) for mouse targeting
- Comfortable for trackpad users (larger target)

#### StandardPadding (8px)
**Problem**: Need breathing room between elements without wasting space.

**Solution**: 8px padding
- Matches ImGui's default spacing (FramePadding.Y × 2)
- Creates visual grouping without excessive gaps
- Maintains information density in panels
- Even multiple of 4 (aligns well with pixel grid)

**Alternatives Considered**:
- 4px: Too cramped, elements feel crowded
- 12px: Too spacious, reduces visible content
- 10px: Odd number, alignment issues with some layouts

#### Axis Colors (Desaturated)
**Problem**: Pure colors (1.0, 0.0, 0.0) are harsh on eyes in dark themes.

**Solution**: Desaturated values (0.8, 0.2, 0.2)
- Softer appearance, less eye strain
- Still clearly distinguishable
- Better contrast against dark backgrounds
- Similar to Blender's axis coloring

**Color Theory**:
- Pure red (255,0,0) can cause visual fatigue
- Desaturated red (204,51,51) maintains hue while reducing intensity
- Green already less harsh to human eye (more sensitive to green wavelengths)

### Consistency Benefits

Using constants provides:
1. **Global Style Changes**: Modify one constant to update all UI
2. **Visual Consistency**: All buttons same size, all spacing uniform
3. **Reduced Cognitive Load**: Developers don't decide spacing per UI element
4. **Easier Maintenance**: Search "StandardPadding" finds all uses
5. **Better Code Reviews**: Magic numbers stand out as violations

### Usage Rules

**Golden Rules**:
- ❌ NEVER hardcode sizes: `new Vector2(120, 30)`
- ✅ ALWAYS use constants: `new Vector2(EditorUIConstants.StandardButtonWidth, EditorUIConstants.StandardButtonHeight)`
- ❌ NEVER hardcode colors: `new Vector4(1, 0, 0, 1)`
- ✅ ALWAYS use constants: `EditorUIConstants.ErrorColor`
- ❌ NEVER hardcode spacing: `ImGui.Dummy(new Vector2(0, 10))`
- ✅ ALWAYS use constants: `LayoutDrawer.DrawSpacing(EditorUIConstants.StandardPadding)`

**Exception**: Constants class itself
- EditorUIConstants is the ONLY static class allowed in the codebase
- All other code uses dependency injection (DryIoc)
- Constants are static for ergonomics (no need to inject everywhere)

---

## Quick Reference

### Most Common Constants

```csharp
// Buttons
EditorUIConstants.StandardButtonWidth;  // 120
EditorUIConstants.StandardButtonHeight; // 30

// Layout
EditorUIConstants.PropertyLabelRatio;   // 0.33f
EditorUIConstants.StandardPadding;      // 8

// Colors
EditorUIConstants.ErrorColor;           // Red (1,0,0,1)
EditorUIConstants.SuccessColor;         // Green (0,1,0,1)
EditorUIConstants.AxisXColor;           // Red (0.8,0.2,0.2,1)
EditorUIConstants.AxisYColor;           // Green (0.2,0.8,0.2,1)
EditorUIConstants.AxisZColor;           // Blue (0.2,0.2,0.8,1)

// Buffers
EditorUIConstants.MaxNameLength;        // 128
EditorUIConstants.MaxPathLength;        // 512
```

### Usage Cheat Sheet

```csharp
// Standard button
ButtonDrawer.DrawButton("Save"); // Uses StandardButton Width/Height automatically

// Colored button
ButtonDrawer.DrawColoredButton("Delete", MessageType.Error); // Uses ErrorColor

// Standard spacing
LayoutDrawer.DrawSpacing(); // Uses StandardPadding (8px)

// Property layout
float labelWidth = totalWidth * EditorUIConstants.PropertyLabelRatio;

// Input buffer
private readonly byte[] _buffer = new byte[EditorUIConstants.MaxNameLength];
```
