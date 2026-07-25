## Component Editors

### Component Editors Pattern
Every component editor must implement IComponentEditor and wrap UI in ComponentEditorRegistry.DrawComponent<T>(). Use UIPropertyRenderer for primitives and VectorPanel for vectors; do not use IFieldEditor in component editors.

**Sources:** code-patterns, documentation (confidence 94%)

```csharp
public class TransformComponentEditor : ComponentEditor<TransformComponent>
protected override void DrawContent(TransformComponent component, Entity entity) { ... }
```

### IFieldEditor Only For Script Inspector
IFieldEditor is non-generic and boxing-based for reflection-discovered script fields only. Component editors must use UIPropertyRenderer/VectorPanel instead.

**Sources:** documentation (confidence 90%)

```csharp
Wrong in component editor: _floatEditor.DrawField("Speed", ref component.Speed)
Correct: UIPropertyRenderer.DrawPropertyField("Speed", component.Speed, v => component.Speed = (float)v)
```
