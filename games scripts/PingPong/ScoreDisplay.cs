using System;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class ScoreDisplay : ScriptableEntity
{
    // Public fields (editable in editor)
    public float displayUpdateInterval = 0.1f; // How often to update display
    public bool showDebugInfo = true;
    public bool showInstructions = true;
    
    // Private fields
    private Entity gameManagerEntity;
    private PingPongGameManager gameManager;
    private float updateTimer = 0f;
    private bool hasConnectedToGameManager = false;
    
    // Display strings (could be used with UI text components)
    private string scoreText = "";
    private string stateText = "";
    private string instructionText = "";
    
    public override void OnCreate()
    {
        Console.WriteLine("[ScoreDisplay] Score Display initialized");
        
        // Find and connect to Game Manager
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
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
        updateTimer += deltaTime;
        
        // Try to connect to game manager if not connected
        if (!hasConnectedToGameManager)
        {
            ConnectToGameManager();
            return;
        }
        
        // Update display at intervals
        if (updateTimer >= displayUpdateInterval)
        {
            UpdateDisplayStrings();
            updateTimer = 0f;
        }
    }
    
    private void ConnectToGameManager()
    {
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                gameManager = manager;
                hasConnectedToGameManager = true;
                Console.WriteLine("[ScoreDisplay] Connected to Game Manager");
            }
        }
    }
    
    private void UpdateDisplayStrings()
    {
        if (gameManager == null) return;
        
        var gameState = gameManager.GetGameState();
        var player1Score = gameManager.GetPlayer1Score();
        var player2Score = gameManager.GetPlayer2Score();
        var gameTime = gameManager.GetGameTime();
        var isPlayer1Turn = gameManager.IsPlayer1Turn();
        
        // Update score text
        scoreText = $"Player 1: {player1Score}  |  Player 2: {player2Score}";
        
        // Update state text
        stateText = gameState switch
        {
            PingPongGameState.Menu => "PING PONG - Press SPACE to Start",
            PingPongGameState.Serving => $"Player {(isPlayer1Turn ? "1" : "2")} Serves - Press SPACE",
            PingPongGameState.Playing => $"Playing - Time: {gameTime:F1}s",
            PingPongGameState.Paused => "PAUSED - Press P to Resume",
            PingPongGameState.GameOver => DetermineWinner(player1Score, player2Score),
            _ => "Unknown State"
        };
        
        // Update instruction text
        if (showInstructions)
        {
            instructionText = gameState switch
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
            Console.WriteLine($"    {scoreText}");
            Console.WriteLine($"    {stateText}");
            if (showInstructions && !string.IsNullOrEmpty(instructionText))
            {
                Console.WriteLine($"    {instructionText}");
            }
            Console.WriteLine("========================================");
        }
        
        // Always output debug info if enabled
        if (showDebugInfo && gameManager != null)
        {
            OutputDebugInfo();
        }
    }
    
    private bool ShouldOutputToConsole()
    {
        // This is a simple implementation - you might want to track previous state
        // and only output when something changes
        return gameManager != null && 
               (gameManager.GetGameState() == PingPongGameState.Menu ||
                gameManager.GetGameState() == PingPongGameState.GameOver ||
                gameManager.GetGameState() == PingPongGameState.Serving);
    }
    
    private void OutputDebugInfo()
    {
        var gameState = gameManager.GetGameState();
        var gameTime = gameManager.GetGameTime();
        
        // Only output debug info occasionally to avoid spam
        if (updateTimer <= 0.01f) // Only on first update of interval
        {
            Console.WriteLine($"[ScoreDisplay] Debug - State: {gameState}, Time: {gameTime:F1}s");
        }
    }
    
    // Public methods for UI system integration
    public string GetScoreText() => scoreText;
    public string GetStateText() => stateText;
    public string GetInstructionText() => instructionText;
    
    public void SetDisplayUpdateInterval(float interval)
    {
        displayUpdateInterval = Math.Max(0.05f, interval);
    }
    
    public void EnableDebugInfo(bool enable)
    {
        showDebugInfo = enable;
    }
    
    public void EnableInstructions(bool enable)
    {
        showInstructions = enable;
    }
    
    // Method to get formatted game info for external systems
    public string GetFormattedGameInfo()
    {
        if (gameManager == null) return "Game Manager not connected";
        
        var player1Score = gameManager.GetPlayer1Score();
        var player2Score = gameManager.GetPlayer2Score();
        var gameState = gameManager.GetGameState();
        var gameTime = gameManager.GetGameTime();
        
        return $"Score: {player1Score}-{player2Score} | State: {gameState} | Time: {gameTime:F1}s";
    }
}