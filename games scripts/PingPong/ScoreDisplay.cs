using System;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class ScoreDisplay : ScriptableEntity
{
    // Public fields (editable in editor)
    public float DisplayUpdateInterval = 0.1f; // How often to update display
    public bool ShowDebugInfo = true;
    public bool ShowInstructions = true;
    
    // Private fields
    private Entity _gameManagerEntity;
    private PingPongGameManager _gameManager;
    private float _updateTimer = 0f;
    private bool _hasConnectedToGameManager = false;
    
    // Display strings (could be used with UI text components)
    private string _scoreText = "";
    private string _stateText = "";
    private string _instructionText = "";
    
    public override void OnCreate()
    {
        Console.WriteLine("[ScoreDisplay] Score Display initialized");
        
        // Find and connect to Game Manager
        _gameManagerEntity = FindEntity("Game Manager");
        if (_gameManagerEntity != null)
        {
            var scriptComponent = _gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                _gameManager = manager;
                _hasConnectedToGameManager = true;
                Console.WriteLine("[ScoreDisplay] Connected to Game Manager");
            }
        }
        else
        {
            Console.WriteLine("[ScoreDisplay] WARNING: Game Manager not found!");
        }
        
        UpdateDisplayStrings();
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        _updateTimer += deltaTime;
        
        // Try to connect to game manager if not connected
        if (!_hasConnectedToGameManager)
        {
            ConnectToGameManager();
            return;
        }
        
        // Update display at intervals
        if (_updateTimer >= DisplayUpdateInterval)
        {
            UpdateDisplayStrings();
            _updateTimer = 0f;
        }
    }
    
    private void ConnectToGameManager()
    {
        _gameManagerEntity = FindEntity("Game Manager");
        if (_gameManagerEntity != null)
        {
            var scriptComponent = _gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                _gameManager = manager;
                _hasConnectedToGameManager = true;
                Console.WriteLine("[ScoreDisplay] Connected to Game Manager");
            }
        }
    }
    
    private void UpdateDisplayStrings()
    {
        if (_gameManager == null) return;
        
        var gameState = _gameManager.GetGameState();
        var player1Score = _gameManager.GetPlayer1Score();
        var player2Score = _gameManager.GetPlayer2Score();
        var gameTime = _gameManager.GetGameTime();
        var isPlayer1Turn = _gameManager.IsPlayer1Turn();
        
        // Update score text
        _scoreText = $"Player 1: {player1Score}  |  Player 2: {player2Score}";
        
        // Update state text
        _stateText = gameState switch
        {
            PingPongGameState.Menu => "PING PONG - Press SPACE to Start",
            PingPongGameState.Serving => $"Player {(isPlayer1Turn ? "1" : "2")} Serves - Press SPACE",
            PingPongGameState.Playing => $"Playing - Time: {gameTime:F1}s",
            PingPongGameState.Paused => "PAUSED - Press P to Resume",
            PingPongGameState.GameOver => DetermineWinner(player1Score, player2Score),
            _ => "Unknown State"
        };
        
        // Update instruction text
        if (ShowInstructions)
        {
            _instructionText = gameState switch
            {
                PingPongGameState.Menu => "Controls: Player 1 (W/S), Player 2 (Up/Down), Space to Start",
                PingPongGameState.Serving => "Press SPACE to serve or wait for auto-serve",
                PingPongGameState.Playing => "P to Pause, ESC for Menu",
                PingPongGameState.Paused => "P to Resume, ESC for Menu",
                PingPongGameState.GameOver => "R to Restart, ESC for Menu",
                _ => ""
            };
        }
        
        // Output to console (could be replaced with UI system)
        OutputToConsole();
    }
    
    private string DetermineWinner(int player1Score, int player2Score)
    {
        if (player1Score > player2Score)
            return $"PLAYER 1 WINS! ({player1Score}-{player2Score}) - Press R to Restart";
        else
            return $"PLAYER 2 WINS! ({player1Score}-{player2Score}) - Press R to Restart";
    }
    
    private void OutputToConsole()
    {
        // Only output significant state changes to avoid spam
        if (ShouldOutputToConsole())
        {
            Console.WriteLine("========================================");
            Console.WriteLine($"    {_scoreText}");
            Console.WriteLine($"    {_stateText}");
            if (ShowInstructions && !string.IsNullOrEmpty(_instructionText))
            {
                Console.WriteLine($"    {_instructionText}");
            }
            Console.WriteLine("========================================");
        }
        
        // Always output debug info if enabled
        if (ShowDebugInfo && _gameManager != null)
        {
            OutputDebugInfo();
        }
    }
    
    private bool ShouldOutputToConsole()
    {
        // This is a simple implementation - you might want to track previous state
        // and only output when something changes
        return _gameManager != null && 
               (_gameManager.GetGameState() == PingPongGameState.Menu ||
                _gameManager.GetGameState() == PingPongGameState.GameOver ||
                _gameManager.GetGameState() == PingPongGameState.Serving);
    }
    
    private void OutputDebugInfo()
    {
        var gameState = _gameManager.GetGameState();
        var gameTime = _gameManager.GetGameTime();
        
        // Only output debug info occasionally to avoid spam
        if (_updateTimer <= 0.01f) // Only on first update of interval
        {
            Console.WriteLine($"[ScoreDisplay] Debug - State: {gameState}, Time: {gameTime:F1}s");
        }
    }
    
    // Public methods for UI system integration
    public string GetScoreText() => _scoreText;
    public string GetStateText() => _stateText;
    public string GetInstructionText() => _instructionText;
    
    public void SetDisplayUpdateInterval(float interval)
    {
        DisplayUpdateInterval = Math.Max(0.05f, interval);
    }
    
    public void EnableDebugInfo(bool enable)
    {
        ShowDebugInfo = enable;
    }
    
    public void EnableInstructions(bool enable)
    {
        ShowInstructions = enable;
    }
    
    // Method to get formatted game info for external systems
    public string GetFormattedGameInfo()
    {
        if (_gameManager == null) return "Game Manager not connected";
        
        var player1Score = _gameManager.GetPlayer1Score();
        var player2Score = _gameManager.GetPlayer2Score();
        var gameState = _gameManager.GetGameState();
        var gameTime = _gameManager.GetGameTime();
        
        return $"Score: {player1Score}-{player2Score} | State: {gameState} | Time: {gameTime:F1}s";
    }
}