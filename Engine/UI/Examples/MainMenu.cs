using System;
using System.Numerics;
using Engine.Renderer;
using Engine.UI.Elements;

namespace Engine.UI.Examples;

public class MainMenu
{
    private UIManager _uiManager;
    private Button _playButton;
    private Button _optionsButton;
    private Button _exitButton;
    private Text _titleText;
    
    public Action? OnPlayClicked { get; set; }
    public Action? OnOptionsClicked { get; set; }
    public Action? OnExitClicked { get; set; }
    
    public MainMenu(IGraphics2D graphics2D)
    {
        _uiManager = new UIManager(graphics2D);
        CreateMenuElements();
    }
    
    private void CreateMenuElements()
    {
        // Title Text
        _titleText = new Text("Game Engine Demo")
        {
            Id = "title",
            Position = new Vector2(0.5f, 0.2f), // Center horizontally, 20% from top
            Alignment = TextAlignment.Center,
            FontSize = 32,
            ZOrder = 1
        };
        _titleText.Style.TextColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // White
        _uiManager.AddElement(_titleText);
        
        // Play Button
        _playButton = new Button("Play Game")
        {
            Id = "play-button",
            Position = new Vector2(0.5f, 0.4f), // Center horizontally, 40% from top
            Size = new Vector2(0.2f, 0.08f), // 20% width, 8% height
            ZOrder = 2
        };
        _playButton.OnClick = () => OnPlayClicked?.Invoke();
        _uiManager.AddElement(_playButton);
        
        // Options Button
        _optionsButton = new Button("Options")
        {
            Id = "options-button",
            Position = new Vector2(0.5f, 0.55f), // Center horizontally, 55% from top
            Size = new Vector2(0.2f, 0.08f),
            ZOrder = 2
        };
        _optionsButton.OnClick = () => OnOptionsClicked?.Invoke();
        _uiManager.AddElement(_optionsButton);
        
        // Exit Button
        _exitButton = new Button("Exit")
        {
            Id = "exit-button",
            Position = new Vector2(0.5f, 0.7f), // Center horizontally, 70% from top
            Size = new Vector2(0.2f, 0.08f),
            ZOrder = 2
        };
        _exitButton.OnClick = () => OnExitClicked?.Invoke();
        _uiManager.AddElement(_exitButton);
        
        // Center all buttons horizontally (subtract half width for proper centering)
        _playButton.Position = new Vector2(0.5f - _playButton.Size.X * 0.5f, _playButton.Position.Y);
        _optionsButton.Position = new Vector2(0.5f - _optionsButton.Size.X * 0.5f, _optionsButton.Position.Y);
        _exitButton.Position = new Vector2(0.5f - _exitButton.Size.X * 0.5f, _exitButton.Position.Y);
    }
    
    public void Update(float deltaTime)
    {
        _uiManager.Update(deltaTime);
    }
    
    public void Render()
    {
        _uiManager.Render();
    }
    
    public void SetScreenSize(Vector2 screenSize)
    {
        _uiManager.SetScreenSize(screenSize);
    }
    
    public bool HandleMouseClick(Vector2 mousePosition)
    {
        return _uiManager.HandleMouseClick(mousePosition);
    }
    
    public bool HandleMouseMove(Vector2 mousePosition)
    {
        return _uiManager.HandleMouseMove(mousePosition);
    }
    
    public void Show()
    {
        foreach (var element in _uiManager.Elements)
        {
            element.Visible = true;
        }
    }
    
    public void Hide()
    {
        foreach (var element in _uiManager.Elements)
        {
            element.Visible = false;
        }
    }
    
    public UIManager UIManager => _uiManager;
}