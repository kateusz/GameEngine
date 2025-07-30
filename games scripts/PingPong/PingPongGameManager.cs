using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public enum PingPongGameState
{
    Menu,
    Playing,
    Paused,
    GameOver,
    Serving
}

public class PingPongGameManager : ScriptableEntity
{
    // Public fields (editable in editor)
    public int winningScore = 11;
    public float serveDelay = 2.0f;
    public bool enableAI = true;
    public float aiDifficulty = 1.0f; // 0.5 = easy, 1.0 = normal, 1.5 = hard
    
    // Game state
    private PingPongGameState gameState = PingPongGameState.Menu;
    private int player1Score = 0;
    private int player2Score = 0;
    private bool player1Turn = true; // Whose turn it is to serve
    private float serveTimer = 0f;
    private float gameTime = 0f;
    
    // Entity references
    private Entity ballEntity;
    private Entity player1PaddleEntity;
    private Entity player2PaddleEntity;
    
    // Script references
    private BallController ballController;
    private PaddleController player1Controller;
    private PaddleController player2Controller;
    private AIController aiController;
    
    // Debug settings
    private float debugLogTimer = 0f;
    private bool enableDebugLogs = false;
    
    public override void OnCreate()
    {
        Console.WriteLine("[PingPongGameManager] ====== PING PONG GAME MANAGER INITIALIZED ======");
        
        // Find and connect to all game entities
        ConnectToEntities();
        
        // Initialize game state
        SetGameState(PingPongGameState.Menu);
        
        Console.WriteLine("[PingPongGameManager] ====== INITIALIZATION COMPLETE ======");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        
        // Update game time
        if (gameState == PingPongGameState.Playing)
        {
            gameTime += deltaTime;
        }
        
        // Handle state-specific updates
        switch (gameState)
        {
            case PingPongGameState.Menu:
                HandleMenuInput();
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
                if (gameState == PingPongGameState.Menu)
                    StartGame();
                else if (gameState == PingPongGameState.Serving)
                    ServeBall();
                break;
                
            case KeyCodes.P:
                if (gameState == PingPongGameState.Playing)
                    PauseGame();
                else if (gameState == PingPongGameState.Paused)
                    ResumeGame();
                break;
                
            case KeyCodes.R:
                if (gameState == PingPongGameState.GameOver)
                    RestartGame();
                break;
                
            case KeyCodes.Escape:
                if (gameState != PingPongGameState.Menu)
                    ReturnToMenu();
                break;
        }
    }
    
    private void ConnectToEntities()
    {
        // Find ball
        ballEntity = FindEntity("Ball");
        if (ballEntity != null)
        {
            var scriptComponent = ballEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is BallController ball)
            {
                ballController = ball;
                Console.WriteLine("[PingPongGameManager] Connected to Ball");
            }
        }
        
        // Find Player 1 paddle
        player1PaddleEntity = FindEntity("Player1Paddle");
        if (player1PaddleEntity != null)
        {
            var scriptComponent = player1PaddleEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PaddleController paddle1)
            {
                player1Controller = paddle1;
                Console.WriteLine("[PingPongGameManager] Connected to Player 1 Paddle");
            }
        }
        
        // Find Player 2 paddle (could be AI or human)
        player2PaddleEntity = FindEntity("Player2Paddle");
        if (player2PaddleEntity != null)
        {
            var scriptComponent = player2PaddleEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is AIController ai)
            {
                aiController = ai;
                Console.WriteLine("[PingPongGameManager] Connected to AI Controller");
            }
            else if (scriptComponent?.ScriptableEntity is PaddleController paddle2)
            {
                player2Controller = paddle2;
                Console.WriteLine("[PingPongGameManager] Connected to Player 2 Paddle");
            }
        }
    }
    
    private void HandleMenuInput()
    {
        // Menu is handled by OnKeyPressed
    }
    
    private void HandleServing(float deltaTime)
    {
        serveTimer += deltaTime;
        
        if (serveTimer >= serveDelay)
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
        // Game over input is handled by OnKeyPressed
    }
    
    public void StartGame()
    {
        Console.WriteLine("[PingPongGameManager] ====== STARTING NEW GAME ======");
        
        // Reset scores
        player1Score = 0;
        player2Score = 0;
        gameTime = 0f;
        player1Turn = true;
        
        // Reset all entities
        ResetGameEntities();
        
        // Set AI difficulty
        if (aiController != null)
        {
            SetAIDifficulty(aiDifficulty);
        }
        
        // Start serving
        SetGameState(PingPongGameState.Serving);
        
        Console.WriteLine("[PingPongGameManager] ====== GAME STARTED ======");
    }
    
    public void ServeBall()
    {
        Console.WriteLine($"[PingPongGameManager] Serving ball - Player {(player1Turn ? "1" : "2")}'s turn");
        
        ballController?.Launch(!player1Turn); // Launch towards the other player
        SetGameState(PingPongGameState.Playing);
    }
    
    public void Player1Score()
    {
        player1Score++;
        Console.WriteLine($"[PingPongGameManager] ====== PLAYER 1 SCORES! ======");
        Console.WriteLine($"[PingPongGameManager] Score: Player 1: {player1Score} - Player 2: {player2Score}");
        
        OnScore(true);
    }
    
    public void Player2Score()
    {
        player2Score++;
        Console.WriteLine($"[PingPongGameManager] ====== PLAYER 2 SCORES! ======");
        Console.WriteLine($"[PingPongGameManager] Score: Player 1: {player1Score} - Player 2: {player2Score}");
        
        OnScore(false);
    }
    
    private void OnScore(bool player1Scored)
    {
        // Check for game end
        if (player1Score >= winningScore || player2Score >= winningScore)
        {
            EndGame();
            return;
        }
        
        // Switch serve
        player1Turn = player1Scored;
        
        // Reset for next serve
        ResetGameEntities();
        SetGameState(PingPongGameState.Serving);
    }
    
    private void EndGame()
    {
        bool player1Won = player1Score >= winningScore;
        
        Console.WriteLine($"[PingPongGameManager] ====== GAME OVER ======");
        Console.WriteLine($"[PingPongGameManager] Winner: Player {(player1Won ? "1" : "2")}!");
        Console.WriteLine($"[PingPongGameManager] Final Score: Player 1: {player1Score} - Player 2: {player2Score}");
        Console.WriteLine($"[PingPongGameManager] Game Duration: {gameTime:F2} seconds");
        
        ballController?.Stop();
        SetGameState(PingPongGameState.GameOver);
    }
    
    private void PauseGame()
    {
        Console.WriteLine("[PingPongGameManager] Game paused");
        ballController?.Stop();
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
    
    private void ReturnToMenu()
    {
        Console.WriteLine("[PingPongGameManager] Returning to menu");
        ResetGameEntities();
        SetGameState(PingPongGameState.Menu);
    }
    
    private void ResetGameEntities()
    {
        Console.WriteLine("[PingPongGameManager] Resetting game entities");
        
        ballController?.ResetBall();
        player1Controller?.ResetPosition();
        player2Controller?.ResetPosition();
        aiController?.ResetPosition();
    }
    
    private void SetGameState(PingPongGameState newState)
    {
        var oldState = gameState;
        gameState = newState;
        serveTimer = 0f;
        
        Console.WriteLine($"[PingPongGameManager] ====== STATE CHANGE: {oldState} -> {newState} ======");
        
        switch (newState)
        {
            case PingPongGameState.Menu:
                Console.WriteLine("[PingPongGameManager] MENU STATE: Press Space to start");
                break;
                
            case PingPongGameState.Serving:
                Console.WriteLine($"[PingPongGameManager] SERVING STATE: Player {(player1Turn ? "1" : "2")}'s serve - Press Space or wait");
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
    
    private void SetAIDifficulty(float difficulty)
    {
        if (aiController == null) return;
        
        // Adjust AI parameters based on difficulty
        float speed = 6.0f;
        float reaction = 0.005f;
        float prediction = 0.2f * difficulty;
        
        aiController.SetDifficulty(difficulty);
        Console.WriteLine($"[PingPongGameManager] AI difficulty set to {difficulty}");
    }
    
    private void PerformDebugLogging(float deltaTime)
    {
        debugLogTimer += deltaTime;
        
        if (enableDebugLogs && debugLogTimer >= 3.0f)
        {
            Console.WriteLine($"[PingPongGameManager] ====== DEBUG STATUS ======");
            Console.WriteLine($"[PingPongGameManager] State: {gameState}");
            Console.WriteLine($"[PingPongGameManager] Score: {player1Score} - {player2Score}");
            Console.WriteLine($"[PingPongGameManager] Game Time: {gameTime:F2}s");
            Console.WriteLine($"[PingPongGameManager] Serve Turn: Player {(player1Turn ? "1" : "2")}");
            
            if (ballController != null)
            {
                var ballPos = ballController.GetPosition();
                Console.WriteLine($"[PingPongGameManager] Ball: ({ballPos.X:F2}, {ballPos.Y:F2}) - Active: {ballController.IsActive()}");
            }
            
            Console.WriteLine($"[PingPongGameManager] ========================");
            debugLogTimer = 0.0f;
        }
    }
    
    // Public getters for other scripts or UI
    public PingPongGameState GetGameState() => gameState;
    public int GetPlayer1Score() => player1Score;
    public int GetPlayer2Score() => player2Score;
    public float GetGameTime() => gameTime;
    public bool IsPlayer1Turn() => player1Turn;
}