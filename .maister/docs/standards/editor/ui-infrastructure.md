## Editor UI Infrastructure

### Always Use Editor UI Infrastructure
Never reimplement existing UI patterns. Prefer Drawers/Elements/FieldEditors over raw ImGui when a matching helper exists. All panels MUST use this infrastructure.

**Sources:** code-patterns, documentation (confidence 95%)

```csharp
public static class ButtonDrawer { public static bool Draw(...) { ... } }
public static class LayoutDrawer { ... }
```

### EditorUIConstants and No Magic Numbers
Use EditorUIConstants for all sizing, spacing, and colors — never hardcode magic numbers or raw color vectors in editor UI.

**Sources:** documentation, pr-reviews (confidence 84%)

```csharp
Wrong: ImGui.Button("Export", new Vector2(150, 35)); ImGui.Dummy(new Vector2(0, 10))
Correct: ButtonDrawer with EditorUIConstants.WideButtonWidth / LayoutDrawer.DrawSpacing()
```

### Specialized Drop Targets For Assets
Use TextureDropTarget/AudioDropTarget/MeshDropTarget (etc.) for asset references — never hand-roll BeginDragDropTarget validation.

**Sources:** documentation (confidence 90%)

```csharp
TextureDropTarget.Draw("Texture", currentPath, onTextureChanged, assetsManager)
```

### Semantic Colors For Actions
Use MessageType semantic colors for actions: Error/red for destructive, Success/green for confirmations, Warning/yellow for cautions.

**Sources:** documentation (confidence 86%)

```csharp
ButtonDrawer.DrawColoredButton("Delete", MessageType.Error)
```

### Panels Are Singletons With Constructor Injection
Register editor panels as Singleton; inject all dependencies via primary constructor; never create static panel singletons.

**Sources:** documentation (confidence 88%)
