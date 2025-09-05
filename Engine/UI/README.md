# UI System - Phase 1 Implementation

## Overview
This is the Phase 1 implementation of the UI System according to the specification. It provides basic screen-space UI rendering with Button and Text components.

## Features Implemented
- ✅ **UIManager**: Central coordinator for UI elements
- ✅ **UIElement**: Base class for all UI components
- ✅ **UIStyle**: Styling system with colors, textures, and borders
- ✅ **Button**: Interactive clickable component with hover/press states
- ✅ **Text**: Basic text rendering (placeholder implementation)
- ✅ **UIRenderer**: Screen-space projection and rendering
- ✅ **Graphics2D Integration**: Uses existing 2D rendering system

## Basic Usage Example

```csharp
// Create UI manager
var uiManager = new UIManager(Graphics2D.Instance);
uiManager.SetScreenSize(new Vector2(1920, 1080));

// Create a button
var playButton = new Button("Play Game")
{
    Position = new Vector2(0.5f, 0.4f), // Normalized coordinates (0-1)
    Size = new Vector2(0.2f, 0.08f),    // 20% width, 8% height
    OnClick = () => StartGame()
};

// Create text
var title = new Text("Main Menu")
{
    Position = new Vector2(0.5f, 0.2f),
    Alignment = TextAlignment.Center
};

// Add to UI manager
uiManager.AddElement(playButton);
uiManager.AddElement(title);

// In your game loop:
// Update
uiManager.Update(deltaTime);

// Render (after 3D scene)
uiManager.Render();

// Handle input
bool inputConsumed = uiManager.HandleMouseClick(mousePosition);
```

## Coordinate System

The UI system uses normalized coordinates:
- **Position**: (0,0) = top-left corner, (1,1) = bottom-right corner
- **Size**: Relative to screen dimensions (0.5, 0.1) = 50% width, 10% height
- **Input**: Automatic conversion from screen-space to normalized coordinates

## MainMenu Example

```csharp
var mainMenu = new MainMenu(Graphics2D.Instance);

mainMenu.OnPlayClicked = () => {
    // Start game
};

mainMenu.OnExitClicked = () => {
    // Exit application  
};

// In game loop
mainMenu.Update(deltaTime);
mainMenu.Render();
mainMenu.HandleMouseClick(mousePosition);
```

## Styling

```csharp
var button = new Button("Styled Button");

// Use predefined styles
button.Style = UIStyle.Button; // Default button style

// Or customize
button.Style.BackgroundColor = new Vector4(0.2f, 0.4f, 0.8f, 1.0f);
button.Style.HoverBackgroundColor = new Vector4(0.3f, 0.5f, 0.9f, 1.0f);
button.Style.TextColor = Vector4.One;
button.Style.BorderWidth = 2.0f;
button.Style.BorderColor = new Vector4(0.5f, 0.7f, 1.0f, 1.0f);
```

## Integration with Game Loop

```csharp
public class GameLayer : Layer
{
    private UIManager _uiManager;
    
    public override void OnAttach()
    {
        _uiManager = new UIManager(Graphics2D.Instance);
        // Create UI elements...
    }
    
    public override void OnUpdate(Timestep ts)
    {
        // Update game logic first
        // ...
        
        // Update UI
        _uiManager.Update((float)ts.TotalSeconds);
    }
    
    public override void OnRender()
    {
        // Render 3D scene first
        Graphics2D.Instance.BeginScene(camera);
        // ... render game objects
        Graphics2D.Instance.EndScene();
        
        // Render UI overlay
        _uiManager.Render();
    }
    
    public override bool OnMouseButtonPressed(MouseButtonPressedEvent e)
    {
        var mousePos = new Vector2(e.GetX(), e.GetY());
        return _uiManager.HandleMouseClick(mousePos);
    }
}
```

## Phase 1 Limitations

1. **Text Rendering**: Currently placeholder implementation using colored quads
2. **Font System**: Not yet implemented (Phase 2)
3. **Input System**: Basic mouse support only
4. **Layout System**: Manual positioning only
5. **Animation**: Not yet implemented

## Next Steps (Phase 2)

- Implement proper font loading and text rendering
- Add glyph atlas generation
- Text measurement and proper sizing
- Multi-line text support

## File Structure

```
Engine/UI/
├── UIManager.cs           # Central UI coordinator
├── UIElement.cs           # Base UI component class
├── UIStyle.cs             # Styling system
├── UIRenderer.cs          # Screen-space rendering
├── Elements/
│   ├── Button.cs          # Interactive button component
│   └── Text.cs            # Text component (placeholder)
├── Examples/
│   └── MainMenu.cs        # Example main menu implementation
└── README.md              # This documentation
```

## Testing

To test the UI system:

1. Create a simple scene with MainMenu
2. Verify buttons respond to mouse hover/click
3. Check screen-space positioning works correctly
4. Test input consumption (UI blocks game input when clicked)
5. Verify rendering order (UI renders on top of 3D scene)