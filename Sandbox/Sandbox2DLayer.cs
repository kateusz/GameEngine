using System.Numerics;
using Engine.Core;
using Engine.Core.Input;
using Engine.Events;
using Engine.Renderer;
using Engine.Renderer.Cameras;
using Engine.Renderer.Textures;
using Engine.UI;
using Engine.UI.Elements;
using NLog;

namespace Sandbox;

public class Sandbox2DLayer : Layer
{
    private const int _mapWidth = 24;
    private const int _mapHeight = 24;

    private const string _mapTiles =
        """
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        WDWDWDWDWDWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        WWWWWWWDDDDWWWWWWWWWWWWW
        WWWWWWWWWWWWWWWWWWWWWWWW
        DWWWWWWWWWWWWWWDDDDWWWWW
        """;


    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private OrthographicCameraController _orthographicCameraController;
    private Texture2D _spriteSheet;
    private SubTexture2D _textureBarrel;

    private readonly Dictionary<char, SubTexture2D> _textureMap = new();
    private readonly char[,] _mapArray;
    
    // UI System Integration
    private UIManager _uiManager;
    private Button _testButton;
    private Button _styledButton;
    private Button _cameraButton;
    private Text _statusText;
    private Text _instructionText;
    private int _clickCount = 0;
    private bool _showUI = true;

    public Sandbox2DLayer() : base("Sandbox2DLayer")
    {
        _mapArray = ConvertMapTo2DArray(_mapTiles, _mapWidth, _mapHeight);
    }

    public override void OnAttach()
    {
        Logger.Debug("Sandbox2DLayer OnAttach.");

        _orthographicCameraController = new OrthographicCameraController(3840.0f / 2160.0f, true);
        _spriteSheet = TextureFactory.Create("assets/game/textures/RPGpack_sheet_2X.png");

        _textureBarrel =
            SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(8, 2), new Vector2(128, 128), new Vector2(1, 1));

        _textureMap['D'] =
            SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(6, 11), new Vector2(128, 128), new Vector2(1, 1));
        _textureMap['W'] = SubTexture2D.CreateFromCoords(_spriteSheet, new Vector2(11, 11), new Vector2(128, 128),
            new Vector2(1, 1));
            
        // Initialize UI System
        InitializeUI();
    }

    public override void OnDetach()
    {
        Logger.Debug("Sandbox2DLayer OnDetach.");
    }

    public override void OnUpdate(TimeSpan timeSpan)
    {
        _orthographicCameraController.OnUpdate(timeSpan);
        
        // Update UI System
        if (_showUI)
        {
            _uiManager.Update((float)timeSpan.TotalSeconds);
        }
        
        Graphics2D.Instance.SetClearColor(new Vector4(0.1f, 0.1f, 0.1f, 1.0f));
        Graphics2D.Instance.Clear();

        Graphics2D.Instance.BeginScene(_orthographicCameraController.Camera);
        
        // Render UI System (overlay on top of 3D scene)
        if (_showUI)
        {
            _uiManager.Render();
        }

        Graphics2D.Instance.DrawQuad(Vector2.Zero, Vector2.One, new Vector4(100, 100, 100, 100));

        Graphics2D.Instance.DrawLine(Vector3.Zero, new Vector3(5, 5, 0), new Vector4(100, 100, 100, 100), 5);
        Graphics2D.Instance.DrawRect(Vector3.Zero, new Vector2(5, 5), new Vector4(100, 100, 100, 100), 5);

        // for (var row = 0; row < _mapHeight; row++)
        // {
        //     for (var col = 0; col < _mapWidth; col++)
        //     {
        //         var subTextureCode = _mapArray[row, col];
        //         var subTexture = _textureMap[subTextureCode];
        //         
        //         // _mapHeight - row because openGl reads from bottom left to top right
        //         Renderer2D.Instance.DrawQuad(new Vector3(col - _mapWidth / 2.0f, _mapHeight - row / 2.0f, 0.5f), new Vector2(1, 1), rotation: 0, subTexture);
        //     }
        // }

        Graphics2D.Instance.EndScene();
    }

    public override void HandleEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);

        // Handle UI input first
        bool uiConsumed = false;
        if (_showUI)
        {
            uiConsumed = HandleUIEvent(@event);
        }
        
        // Only pass event to camera controller if UI didn't consume it
        if (!uiConsumed)
        {
            _orthographicCameraController.OnEvent(@event);
        }
    }

    public override void OnImGuiRender()
    {
    }

    private void InitializeUI()
    {
        Logger.Info("Initializing UI System");
        
        // Create UI manager
        _uiManager = new UIManager(Graphics2D.Instance);
        _uiManager.SetScreenSize(new Vector2(1920, 1080)); // Default screen size
        
        // Create instruction text
        _instructionText = new Text("UI System Demo - Press 'U' to toggle UI | WASD to move camera")
        {
            Id = "instructions",
            Position = new Vector2(0.02f, 0.02f), // Top-left corner
            FontSize = 14
        };
        _instructionText.Style.TextColor = new Vector4(0.0f, 1.0f, 0.0f, 1.0f); // Green
        _uiManager.AddElement(_instructionText);
        
        // Create status text
        _statusText = new Text($"UI System Ready | Clicks: {_clickCount}")
        {
            Id = "status",
            Position = new Vector2(0.02f, 0.08f),
            FontSize = 16
        };
        _statusText.Style.TextColor = new Vector4(0, 1, 0, 1); // Green
        _uiManager.AddElement(_statusText);
        
        // Create test button
        _testButton = new Button("Click Me!")
        {
            Id = "test-button",
            Position = new Vector2(0.02f, 0.15f),
            Size = new Vector2(0.15f, 0.06f),
            OnClick = () => {
                _clickCount++;
                UpdateStatusText();
                Logger.Info($"Test button clicked! Count: {_clickCount}");
            }
        };
        _uiManager.AddElement(_testButton);
        
        // Create styled button
        _styledButton = new Button("Styled")
        {
            Id = "styled-button",
            Position = new Vector2(0.02f, 0.23f),
            Size = new Vector2(0.12f, 0.05f)
        };
        
        // Custom styling - red theme
        _styledButton.Style.BackgroundColor = new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
        _styledButton.Style.HoverBackgroundColor = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
        _styledButton.Style.PressedBackgroundColor = new Vector4(0.6f, 0.1f, 0.1f, 1.0f);
        _styledButton.Style.TextColor = Vector4.One;
        _styledButton.Style.BorderWidth = 2.0f;
        _styledButton.Style.BorderColor = new Vector4(1.0f, 0.5f, 0.5f, 1.0f);
        
        _styledButton.OnClick = () => {
            _clickCount++;
            UpdateStatusText();
            Logger.Info("Styled button clicked!");
        };
        _uiManager.AddElement(_styledButton);
        
        // Create camera control button
        _cameraButton = new Button("Reset Camera")
        {
            Id = "camera-button",
            Position = new Vector2(0.02f, 0.30f),
            Size = new Vector2(0.15f, 0.05f),
            OnClick = () => {
                // Reset camera to center - we'll use the Camera property
                _orthographicCameraController.Camera.SetPosition(Vector3.Zero);
                Logger.Info("Camera reset to center");
                UpdateStatusText("Camera reset!");
            }
        };
        
        // Blue theme for camera button
        _cameraButton.Style.BackgroundColor = new Vector4(0.2f, 0.4f, 0.8f, 1.0f);
        _cameraButton.Style.HoverBackgroundColor = new Vector4(0.3f, 0.5f, 0.9f, 1.0f);
        _cameraButton.Style.PressedBackgroundColor = new Vector4(0.1f, 0.3f, 0.7f, 1.0f);
        _uiManager.AddElement(_cameraButton);
        
        Logger.Info("UI System initialized with 5 elements");
    }
    
    private bool HandleUIEvent(Event @event)
    {
        switch (@event)
        {
            case MouseButtonPressedEvent mousePressed:
                // Note: MouseButtonPressedEvent doesn't have position info, we need to get it differently
                // For now, we'll handle mouse input through mouse moved events
                Logger.Debug($"Mouse button {mousePressed.Button} pressed");
                return false; // Don't consume for now
                
            case MouseMovedEvent mouseMoved:
                var movePos = new Vector2(mouseMoved.X, mouseMoved.Y);
                return _uiManager.HandleMouseMove(movePos);
                
            case WindowResizeEvent resize:
                Logger.Info($"Window resized to {resize.Width}x{resize.Height}");
                _uiManager.SetScreenSize(new Vector2(resize.Width, resize.Height));
                return false; // Don't consume resize events
                
            case KeyPressedEvent keyPressed:
                // Toggle UI with 'U' key
                if ((KeyCodes)keyPressed.KeyCode == Engine.Core.Input.KeyCodes.U)
                {
                    _showUI = !_showUI;
                    Logger.Info($"UI visibility toggled: {_showUI}");
                    return true; // Consume the key event
                }
                break;
        }
        
        return false;
    }
    
    private void UpdateStatusText()
    {
        UpdateStatusText($"UI Active | Clicks: {_clickCount}");
    }
    
    private void UpdateStatusText(string message)
    {
        if (_statusText != null)
        {
            _statusText.SetContent(message);
        }
    }

    static char[,] ConvertMapTo2DArray(string mapTiles, int mapWidth, int mapHeight)
    {
        char[,] mapArray = new char[mapHeight, mapWidth];
        string[] rows = mapTiles.Trim().Split('\n');

        for (int row = 0; row < mapHeight; row++)
        {
            for (int col = 0; col < mapWidth; col++)
            {
                mapArray[row, col] = rows[row][col];
            }
        }

        return mapArray;
    }
}