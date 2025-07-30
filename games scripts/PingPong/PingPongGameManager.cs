using System;
using System.IO;
using System.Numerics;
using ECS;
using Editor;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Renderer.Textures;
using Engine.Scene.Components;

public enum PingPongGameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Serving
}

public class MenuButton
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Entity ButtonEntity { get; set; }
    public Action OnClick { get; set; }
    public string ButtonName { get; set; }

    public MenuButton(string name, Vector2 position, Vector2 size, Action onClick)
    {
        ButtonName = name;
        Position = position;
        Size = size;
        OnClick = onClick;
    }

    public bool IsPointInside(Vector2 point)
    {
        return point.X >= Position.X - Size.X / 2 &&
               point.X <= Position.X + Size.X / 2 &&
               point.Y >= Position.Y - Size.Y / 2 &&
               point.Y <= Position.Y + Size.Y / 2;
    }
}

public class PingPongGameManager : ScriptableEntity
{
    // Public fields (editable in editor)
    public int WinningScore = 11;
    public float ServeDelay = 2.0f;
    public bool EnableAi = true;
    public float AiDifficulty = 1.0f; // 0.5 = easy, 1.0 = normal, 1.5 = hard

    // Game state
    private PingPongGameState _gameState = PingPongGameState.Menu;

    // Menu buttons
    private MenuButton _playButton;
    private MenuButton _quitButton;
    private readonly string _playButtonTexturePath = Path.Combine(AssetsManager.AssetsPath, "textures/play.png");
    private readonly string _quitButtonTexturePath = Path.Combine(AssetsManager.AssetsPath, "assets/textures/menu.png");
    
    // Camera reference for world-to-screen conversion
    private Entity _cameraEntity;
    private CameraComponent _cameraComponent;

    private int _player1Score = 0;
    private int _player2Score = 0;
    private bool _player1Turn = true; // Whose turn it is to serve
    private float _serveTimer = 0f;
    private float _gameTime = 0f;

    // Entity references
    private Entity _ballEntity;
    private Entity _player1PaddleEntity;
    private Entity _player2PaddleEntity;

    // Script references
    private BallController _ballController;
    private PaddleController _player1Controller;
    private PaddleController _player2Controller;
    private AIController _aiController;

    // Debug settings
    private float _debugLogTimer = 0f;
    private bool _enableDebugLogs = false;

    public override void OnCreate()
    {
        Console.WriteLine("[PingPongGameManager] ====== PING PONG GAME MANAGER INITIALIZED ======");

        // Find and connect to all game entities
        ConnectToEntities();

        // Find camera for coordinate conversion
        FindCamera();

        // Initialize menu buttons
        InitializeMenuButtons();

        // Initialize game state
        SetGameState(PingPongGameState.Menu);

        Console.WriteLine("[PingPongGameManager] ====== INITIALIZATION COMPLETE ======");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;

        // Update game time
        if (_gameState == PingPongGameState.Playing)
        {
            _gameTime += deltaTime;
        }

        // Handle state-specific updates
        switch (_gameState)
        {
            case PingPongGameState.Menu:
                DisplayMenu();
                break;

            case PingPongGameState.Serving:
                HandleServing(deltaTime);
                break;

            case PingPongGameState.Playing:
                HandleGameplay();
                break;

            case PingPongGameState.Paused:
                HandlePauseInput();
                break;

            case PingPongGameState.GameOver:
                HandleGameOverInput();
                break;
        }

        // Debug logging
        PerformDebugLogging(deltaTime);
    }

    public override void OnKeyPressed(KeyCodes key)
    {
        switch (key)
        {
            case KeyCodes.Space:
                if (_gameState == PingPongGameState.Menu)
                    StartGame();
                else if (_gameState == PingPongGameState.Serving)
                    ServeBall();
                break;

            case KeyCodes.P:
                if (_gameState == PingPongGameState.Playing)
                    PauseGame();
                else if (_gameState == PingPongGameState.Paused)
                    ResumeGame();
                break;

            case KeyCodes.R:
                if (_gameState == PingPongGameState.GameOver)
                    RestartGame();
                break;

            case KeyCodes.Escape:
                if (_gameState != PingPongGameState.Menu)
                    ReturnToMenu();
                break;
        }
    }

    public override void OnMouseButtonPressed(int button)
    {
        if (button == 0) // Left mouse button
        {
            HandleMouseClick();
        }
    }

    private void FindCamera()
    {
        // Search for a camera entity in the scene
        // You might need to adjust this based on your scene setup
        _cameraEntity = FindEntity("Camera"); // or whatever your camera is named

        if (_cameraEntity != null && _cameraEntity.HasComponent<CameraComponent>())
        {
            _cameraComponent = _cameraEntity.GetComponent<CameraComponent>();
            Console.WriteLine("[PingPongGameManager] Camera found and connected!");
        }
        else
        {
            Console.WriteLine("[PingPongGameManager] Warning: No camera found! Click detection may not work properly.");
        }
    }

    private void InitializeMenuButtons()
    {
        Console.WriteLine("[PingPongGameManager] Creating menu buttons...");

        // Create Play Button
        _playButton = new MenuButton(
            "PlayButton",
            new Vector2(0.0f, 1.0f), // Position (centered horizontally, above center)
            new Vector2(4.0f, 1.5f), // Size (width, height)
            () => StartGame()
        );

        // Create Quit Button  
        _quitButton = new MenuButton(
            "QuitButton",
            new Vector2(0.0f, -1.0f), // Position (centered horizontally, below center)
            new Vector2(4.0f, 1.5f), // Size (width, height)
            () => QuitGame()
        );

        // Create visual entities for the buttons
        CreateButtonEntity(_playButton, _playButtonTexturePath);
        CreateButtonEntity(_quitButton, _quitButtonTexturePath);

        Console.WriteLine("[PingPongGameManager] Menu buttons created successfully!");
    }

    private void CreateButtonEntity(MenuButton button, string texturePath)
    {
        // Create entity for the button
        var scene = CurrentScene.Instance;
        button.ButtonEntity = scene.CreateEntity(button.ButtonName);

        // Add transform component
        var transform = button.ButtonEntity.AddComponent<TransformComponent>();
        transform.Translation = new Vector3(button.Position.X, button.Position.Y, 0.0f);
        transform.Scale = new Vector3(button.Size.X, button.Size.Y, 1.0f);

        // Add sprite renderer with texture
        var spriteRenderer = button.ButtonEntity.AddComponent<SpriteRendererComponent>();
        try
        {
            spriteRenderer.Texture = TextureFactory.Create(texturePath);
            Console.WriteLine($"[PingPongGameManager] Texture loaded for {button.ButtonName}: {texturePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PingPongGameManager] Failed to load texture for {button.ButtonName}: {ex.Message}");
            // Set a default color if texture loading fails
            spriteRenderer.Color = new Vector4(0.7f, 0.7f, 0.9f, 1.0f); // Light blue
        }

        // Add ID component
        button.ButtonEntity.AddComponent<IdComponent>();

        Console.WriteLine($"[PingPongGameManager] Created button entity: {button.ButtonName}");
    }

    private void ConnectToEntities()
    {
        // Find ball
        _ballEntity = FindEntity("Ball");
        if (_ballEntity != null)
        {
            var scriptComponent = _ballEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is BallController ball)
            {
                _ballController = ball;
                Console.WriteLine("[PingPongGameManager] Connected to Ball");
            }
        }

        // Find Player 1 paddle
        _player1PaddleEntity = FindEntity("Player1Paddle");
        if (_player1PaddleEntity != null)
        {
            var scriptComponent = _player1PaddleEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PaddleController paddle1)
            {
                _player1Controller = paddle1;
                Console.WriteLine("[PingPongGameManager] Connected to Player 1 Paddle");
            }
        }

        // Find Player 2 paddle (could be AI or human)
        _player2PaddleEntity = FindEntity("Player2Paddle");
        if (_player2PaddleEntity != null)
        {
            var scriptComponent = _player2PaddleEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is AIController ai)
            {
                _aiController = ai;
                Console.WriteLine("[PingPongGameManager] Connected to AI Controller");
            }
            else if (scriptComponent?.ScriptableEntity is PaddleController paddle2)
            {
                _player2Controller = paddle2;
                Console.WriteLine("[PingPongGameManager] Connected to Player 2 Paddle");
            }
        }
    }

    private void DisplayMenu()
    {
        // Menu is handled by OnKeyPressed
    }

    private void HandleServing(float deltaTime)
    {
        _serveTimer += deltaTime;

        if (_serveTimer >= ServeDelay)
        {
            // Auto-serve if no input
            ServeBall();
        }
    }

    private void HandleGameplay()
    {
        // Game logic is handled by individual controllers
        // This is where we could add power-ups, special effects, etc.
    }

    private void HandlePauseInput()
    {
        // Pause input is handled by OnKeyPressed
    }

    private void HandleGameOverInput()
    {
        if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.Escape))
        {
            PauseGame();
        }
    }

    private void HandleMouseClick()
    {
        if (_gameState != PingPongGameState.Menu) return;

        // Get mouse position
        var mousePos = InputState.Instance.Mouse.GetPos();
        Vector2 worldPos = ScreenToWorldPosition(mousePos);

        Console.WriteLine(
            $"[PingPongGameManager] Mouse clicked at screen: ({mousePos.X}, {mousePos.Y}), world: ({worldPos.X}, {worldPos.Y})");

        // Check if click is inside any button
        if (_playButton.IsPointInside(worldPos))
        {
            Console.WriteLine("[PingPongGameManager] Play button clicked!");
            _playButton.OnClick?.Invoke();
        }
        else if (_quitButton.IsPointInside(worldPos))
        {
            Console.WriteLine("[PingPongGameManager] Quit button clicked!");
            _quitButton.OnClick?.Invoke();
        }
        else
        {
            Console.WriteLine("[PingPongGameManager] Click missed all buttons");
        }
    }

    private Vector2 ScreenToWorldPosition(Vector2 screenPos)
    {
        // Simple conversion - you may need to adjust this based on your camera setup
        // This assumes an orthographic camera centered at origin
        if (_cameraComponent == null)
        {
            // Fallback: assume screen coordinates match world coordinates
            return new Vector2(screenPos.X - 640, 360 - screenPos.Y); // Assuming 1280x720 screen
        }

        // Get camera properties
        var camera = _cameraComponent.Camera;

        // For orthographic camera, convert screen coordinates to world coordinates
        // You may need to get actual screen/viewport dimensions
        float screenWidth = 1280.0f; // Adjust to your actual screen width
        float screenHeight = 720.0f; // Adjust to your actual screen height

        float orthoSize = camera.OrthographicSize;
        float aspectRatio = screenWidth / screenHeight;

        // Convert screen coordinates to normalized device coordinates (-1 to 1)
        float normalizedX = (screenPos.X / screenWidth) * 2.0f - 1.0f;
        float normalizedY = 1.0f - (screenPos.Y / screenHeight) * 2.0f; // Flip Y

        // Convert to world coordinates
        float worldX = normalizedX * orthoSize * aspectRatio;
        float worldY = normalizedY * orthoSize;

        return new Vector2(worldX, worldY);
    }

    public void StartGame()
    {
        Console.WriteLine("[PingPongGameManager] ====== STARTING NEW GAME ======");

        // Reset scores
        _player1Score = 0;
        _player2Score = 0;
        _gameTime = 0f;
        _player1Turn = true;

        // Reset all entities
        ResetGameEntities();

        // Set AI difficulty
        if (_aiController != null)
        {
            SetAiDifficulty(AiDifficulty);
        }

        // Start serving
        SetGameState(PingPongGameState.Serving);

        Console.WriteLine("[PingPongGameManager] ====== GAME STARTED ======");
    }

    public void ServeBall()
    {
        Console.WriteLine($"[PingPongGameManager] Serving ball - Player {(_player1Turn ? "1" : "2")}'s turn");

        _ballController?.Launch(!_player1Turn); // Launch towards the other player
        SetGameState(PingPongGameState.Playing);
    }

    public void Player1Score()
    {
        _player1Score++;
        Console.WriteLine($"[PingPongGameManager] ====== PLAYER 1 SCORES! ======");
        Console.WriteLine($"[PingPongGameManager] Score: Player 1: {_player1Score} - Player 2: {_player2Score}");

        OnScore(true);
    }

    public void Player2Score()
    {
        _player2Score++;
        Console.WriteLine($"[PingPongGameManager] ====== PLAYER 2 SCORES! ======");
        Console.WriteLine($"[PingPongGameManager] Score: Player 1: {_player1Score} - Player 2: {_player2Score}");

        OnScore(false);
    }

    private void OnScore(bool player1Scored)
    {
        // Check for game end
        if (_player1Score >= WinningScore || _player2Score >= WinningScore)
        {
            EndGame();
            return;
        }

        // Switch serve
        _player1Turn = player1Scored;

        // Reset for next serve
        ResetGameEntities();
        SetGameState(PingPongGameState.Serving);
    }

    private void EndGame()
    {
        bool player1Won = _player1Score >= WinningScore;

        Console.WriteLine($"[PingPongGameManager] ====== GAME OVER ======");
        Console.WriteLine($"[PingPongGameManager] Winner: Player {(player1Won ? "1" : "2")}!");
        Console.WriteLine($"[PingPongGameManager] Final Score: Player 1: {_player1Score} - Player 2: {_player2Score}");
        Console.WriteLine($"[PingPongGameManager] Game Duration: {_gameTime:F2} seconds");

        _ballController?.Stop();
        SetGameState(PingPongGameState.GameOver);
    }

    private void PauseGame()
    {
        Console.WriteLine("[PingPongGameManager] Game paused");
        _ballController?.Stop();
        SetGameState(PingPongGameState.Paused);
    }

    private void ResumeGame()
    {
        Console.WriteLine("[PingPongGameManager] Game resumed");
        SetGameState(PingPongGameState.Playing);
        // Ball will resume automatically in BallController
    }

    private void RestartGame()
    {
        Console.WriteLine("[PingPongGameManager] Restarting game");
        StartGame();
    }

    private void QuitGame()
    {
        Console.WriteLine("[PingPongGameManager] Quitting game...");
        // Implement quit logic (return to main menu, exit application, etc.)
        // For now, just log the action
    }

    private void ReturnToMenu()
    {
        Console.WriteLine("[PingPongGameManager] Returning to menu");
        ResetGameEntities();
        SetGameState(PingPongGameState.Menu);
    }

    private void ResetGameEntities()
    {
        Console.WriteLine("[PingPongGameManager] Resetting game entities");

        _ballController?.ResetBall();
        _player1Controller?.ResetPosition();
        _player2Controller?.ResetPosition();
        _aiController?.ResetPosition();
    }

    private void SetGameState(PingPongGameState newState)
    {
        var oldState = _gameState;
        _gameState = newState;
        _serveTimer = 0f;

        Console.WriteLine($"[PingPongGameManager] ====== STATE CHANGE: {oldState} -> {newState} ======");

        switch (newState)
        {
            case PingPongGameState.Menu:
                RenderMenu();
                Console.WriteLine("[PingPongGameManager] MENU STATE: Press Space to start");
                break;

            case PingPongGameState.Serving:
                Console.WriteLine(
                    $"[PingPongGameManager] SERVING STATE: Player {(_player1Turn ? "1" : "2")}'s serve - Press Space or wait");
                break;

            case PingPongGameState.Playing:
                Console.WriteLine("[PingPongGameManager] PLAYING STATE: Game in progress");
                break;

            case PingPongGameState.Paused:
                Console.WriteLine("[PingPongGameManager] PAUSED STATE: Press P to resume");
                break;

            case PingPongGameState.GameOver:
                Console.WriteLine("[PingPongGameManager] GAME OVER STATE: Press R to restart");
                break;
        }
    }

    private void ShowMenuButtons()
    {
        SetButtonVisibility(true);
        Console.WriteLine("[PingPongGameManager] Menu buttons shown");
    }

    private void HideMenuButtons()
    {
        SetButtonVisibility(false);
        Console.WriteLine("[PingPongGameManager] Menu buttons hidden");
    }

    private void SetButtonVisibility(bool visible)
    {
        // Enable/disable button entities
        if (_playButton?.ButtonEntity != null)
        {
            // You might need to implement entity enabling/disabling in your engine
            // For now, we can move them off-screen or set alpha to 0
            var playTransform = _playButton.ButtonEntity.GetComponent<TransformComponent>();
            if (!visible)
            {
                playTransform.Translation = new Vector3(-1000, -1000, 0); // Move off-screen
            }
            else
            {
                playTransform.Translation = new Vector3(_playButton.Position.X, _playButton.Position.Y, 0);
            }
        }

        if (_quitButton?.ButtonEntity != null)
        {
            var quitTransform = _quitButton.ButtonEntity.GetComponent<TransformComponent>();
            if (!visible)
            {
                quitTransform.Translation = new Vector3(-1000, -1000, 0); // Move off-screen
            }
            else
            {
                quitTransform.Translation = new Vector3(_quitButton.Position.X, _quitButton.Position.Y, 0);
            }
        }
    }

    private void RenderMenu()
    {
        // render two SubTextures,
        // 1
    }

    private void SetAiDifficulty(float difficulty)
    {
        if (_aiController == null) return;

        // Adjust AI parameters based on difficulty
        float speed = 6.0f;
        float reaction = 0.005f;
        float prediction = 0.2f * difficulty;

        _aiController.SetDifficulty(difficulty);
        Console.WriteLine($"[PingPongGameManager] AI difficulty set to {difficulty}");
    }

    private void PerformDebugLogging(float deltaTime)
    {
        _debugLogTimer += deltaTime;

        if (_enableDebugLogs && _debugLogTimer >= 3.0f)
        {
            Console.WriteLine($"[PingPongGameManager] ====== DEBUG STATUS ======");
            Console.WriteLine($"[PingPongGameManager] State: {_gameState}");
            Console.WriteLine($"[PingPongGameManager] Score: {_player1Score} - {_player2Score}");
            Console.WriteLine($"[PingPongGameManager] Game Time: {_gameTime:F2}s");
            Console.WriteLine($"[PingPongGameManager] Serve Turn: Player {(_player1Turn ? "1" : "2")}");

            if (_ballController != null)
            {
                var ballPos = _ballController.GetPosition();
                Console.WriteLine(
                    $"[PingPongGameManager] Ball: ({ballPos.X:F2}, {ballPos.Y:F2}) - Active: {_ballController.IsActive()}");
            }

            Console.WriteLine($"[PingPongGameManager] ========================");
            _debugLogTimer = 0.0f;
        }
    }

    // Public getters for other scripts or UI
    public PingPongGameState GetGameState() => _gameState;
    public int GetPlayer1Score() => _player1Score;
    public int GetPlayer2Score() => _player2Score;
    public float GetGameTime() => _gameTime;
    public bool IsPlayer1Turn() => _player1Turn;
}