# Over-Engineering Audit Results

**Repository:** GameEngine (C# .NET 10.0 + OpenGL)  
**Source Files:** 380 (excl. obj/bin)  
**Total Lines:** 33,268

---

## Findings (Ranked by Impact)

### shrink - Duplicate Logic / Excessive Wrapping

| File | Lines | Action |
|------|-------|--------|
| `./Editor/UI/Drawers/ButtonDrawer.cs` | 345 | Inline 12 button variants directly into call sites |
| `./Editor/UI/Drawers/ModalDrawer.cs` | 437 | Inline modal patterns directly into popup users |
| `./Engine/Scene/Serializer/ComponentDeserializer.cs` | 240 | Merge DeserializeComponent strict/lenient modes (82 line duplication) |
| `./Editor/UI/Drawers/TableDrawer.cs` | 285 | Inline table helpers directly into panels |
| `./Editor/UI/Drawers/LayoutDrawer.cs` | 177 | Inline layout helpers directly into call sites |
| `./Editor/UI/Drawers/DragDropDrawer.cs` | 133 | Inline drag-drop logic into TextureDropTarget/AudioDropTarget |
| `./Editor/UI/Drawers/TreeDrawer.cs` | 140 | Inline tree helpers directly into SceneHierarchyPanel |
| `./Editor/UI/Drawers/TextDrawer.cs` | 47 | Inline 5 one-liners into call sites |
| `./Engine/Scene/Serializer/PrefabSerializer.cs` | 146 | Merge SerializeEntityComponents/ClearEntityComponents duplication (53 lines) |

### yagni - Interface with Single Implementation

| File | Lines | Action |
|------|-------|--------|
| `./Editor/Features/Viewport/ViewportScaleHelper.cs` | 68 | Remove IViewportScaleHelper interface, use ViewportScaleHelper directly |
| `./Editor/Publisher/IGamePublisher.cs` | 40 | Remove interface - GamePublisher has single impl |
| `./Engine/Scene/ISceneSystemRegistry.cs` | 18 | Remove interface - SceneSystemRegistry has single impl |
| `./Editor/ComponentEditors/Core/IComponentEditor.cs` | 8 | Pattern enforced via abstraction bloat - 13 micro-editors |
| `./Editor/ComponentEditors/*.cs` (13 files) | 867 | Inline simple editors into PropertiesPanel call sites |

### delete - Dead Code / Speculative Features

| File | Lines | Action |
|------|-------|--------|
| `./Engine/Renderer/ApiType.cs` | 6 | Enum with single value "SilkNet" - no Vulkan/DX alternatives planned |

### stdlib - Reinvented Standard Library

| File | Lines | Action |
|------|-------|--------|
| `./Engine/Core/Vector2Int.cs` | 53 | Use `Vector2` with int conversion or `System.Drawing.Point` instead |

### native - Over-Abstracted Platform Primitives

| File | Lines | Action |
|------|-------|--------|
| `./Editor/UI/Elements/VectorPanel.cs` | 78 | `ImGui.DragFloat3()` handles vector controls natively |

---

## Summary

| Category | Files | Lines |
|----------|-------|-------|
| shrink | 9 | ~1,388 |
| yagni | 5 | ~993 |
| delete | 1 | 6 |
| stdlib | 1 | 53 |
| native | 1 | 78 |
| **Total** | **17** | **~2,518 lines** |

### Dependencies Removed
- 9 Drawer wrapper dependencies (ButtonDrawer, ModalDrawer, etc.)
- 4 singletons (IViewportScaleHelper, IGamePublisher, ISceneSystemRegistry, KeyModifiers pattern)
- 1 enum (ApiType)
- 12 ComponentEditor files (merged into direct property rendering)

### Recommendation
Start with the Drawer wrappers (1,580 lines) and ComponentDeserializer duplication (82 lines) - these provide the highest ROI for reducing indirection while maintaining the editor UI flexibility.