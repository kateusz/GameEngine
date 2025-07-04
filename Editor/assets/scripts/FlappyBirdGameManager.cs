using System;
using System.Collections.Generic;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Core.Input;
using Engine.Scene.Components;

public enum GameState
{
    Menu,
    Playing,
    GameOver
}

public class FlappyBirdGameManager : ScriptableEntity
{
    private GameState gameState = GameState.Menu;
    private int score = 0;
    private float gameTime = 0.0f;
    private Entity birdEntity;
    private Entity pipeSpawnerEntity;
    
    // Debug logging control
    private bool enableDebugLogs = true;
    private float debugLogInterval = 2.0f; // Log every 2 seconds during gameplay
    private float lastDebugLogTime = 0.0f;
    private int lastScore = -1; // Track score changes
    private GameState lastGameState = GameState.Menu;
    
    public override void OnCreate()
    {
        Console.WriteLine("[DEBUG] Flappy Bird Game Manager initialized!");
        Console.WriteLine($"[DEBUG] Initial game state: {gameState}");
        
        // Find bird and pipe spawner entities
        Console.WriteLine("[DEBUG] Searching for game entities...");
        
        birdEntity = FindEntity("Bird");
        if (birdEntity != null)
        {
            Console.WriteLine($"[DEBUG] Bird entity found: {birdEntity.Name}");
            var birdTransform = birdEntity.GetComponent<TransformComponent>();
            Console.WriteLine($"[DEBUG] Bird initial position: {birdTransform.Translation}");
        }
        else
        {
            Console.WriteLine("[DEBUG] WARNING: Bird entity not found!");
        }
        
        pipeSpawnerEntity = FindEntity("PipeSpawner");
        if (pipeSpawnerEntity != null)
        {
            Console.WriteLine($"[DEBUG] Pipe spawner entity found: {pipeSpawnerEntity.Name}");
        }
        else
        {
            Console.WriteLine("[DEBUG] WARNING: Pipe spawner entity not found!");
        }
        
        // Start in menu state
        Console.WriteLine("[DEBUG] Setting initial game state to Menu");
        SetGameState(GameState.Menu);
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;
        gameTime += deltaTime;
        
        // Debug log periodically
        lastDebugLogTime += deltaTime;
        if (enableDebugLogs && lastDebugLogTime >= debugLogInterval)
        {
            Console.WriteLine($"[DEBUG] Game Update - State: {gameState}, Score: {score}, Game Time: {gameTime:F2}s");
            if (birdEntity != null)
            {
                var birdPos = birdEntity.GetComponent<TransformComponent>().Translation;
                Console.WriteLine($"[DEBUG] Bird position: {birdPos}, Entity status: {(IsBirdDead() ? "DEAD" : "ALIVE")}");
            }
            lastDebugLogTime = 0.0f;
        }
        
        // Log game state changes
        if (gameState != lastGameState)
        {
            Console.WriteLine($"[DEBUG] Game state changed from {lastGameState} to {gameState}");
            lastGameState = gameState;
        }
        
        switch (gameState)
        {
            case GameState.Menu:
                UpdateMenu(ts);
                break;
            case GameState.Playing:
                UpdatePlaying(ts);
                break;
            case GameState.GameOver:
                UpdateGameOver(ts);
                break;
        }
        
        // Update UI display (would need UI system)
        UpdateUI();
    }
    
    private void UpdateMenu(TimeSpan ts)
    {
        if (enableDebugLogs && lastDebugLogTime == 0.0f) // Only log when we just reset the timer
        {
            Console.WriteLine("[DEBUG] Updating Menu state - bird should be bobbing");
        }
        
        // Show instructions, wait for input to start
        // Bird should be stationary or gently bobbing
        if (birdEntity != null)
        {
            var transform = birdEntity.GetComponent<TransformComponent>();
            var oldPosition = transform.Translation;
            var position = transform.Translation;
            position.Y = MathF.Sin(gameTime * 2.0f) * 0.5f; // Gentle bobbing
            transform.Translation = position;
            
            if (enableDebugLogs && Math.Abs(oldPosition.Y - position.Y) > 0.1f)
            {
                Console.WriteLine($"[DEBUG] Menu bobbing - Y position: {oldPosition.Y:F2} -> {position.Y:F2}");
            }
        }
    }
    
    private void UpdatePlaying(TimeSpan ts)
    {
        if (enableDebugLogs && lastDebugLogTime == 0.0f)
        {
            Console.WriteLine("[DEBUG] Updating Playing state - checking scoring and bird status");
        }
        
        // Check for scoring (bird passed through pipes)
        CheckScoring();
        
        // Monitor bird status for game over
        if (IsBirdDead())
        {
            Console.WriteLine("[DEBUG] Bird death detected - transitioning to Game Over");
            SetGameState(GameState.GameOver);
        }
    }
    
    private void UpdateGameOver(TimeSpan ts)
    {
        if (enableDebugLogs && lastDebugLogTime == 0.0f)
        {
            Console.WriteLine($"[DEBUG] Updating Game Over state - Final score: {score}");
        }
        // Display final score, wait for restart input
        // Stop all game objects
    }
    
    public override void OnKeyPressed(KeyCodes key)
    {
        Console.WriteLine($"[DEBUG] Key pressed in Game Manager: {key} (Current state: {gameState})");
        
        switch (gameState)
        {
            case GameState.Menu:
                if (key == KeyCodes.Space)
                {
                    Console.WriteLine("[DEBUG] Space pressed in Menu - starting game");
                    StartGame();
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Key {key} pressed in Menu but not Space - ignoring");
                }
                break;
            case GameState.GameOver:
                if (key == KeyCodes.R || key == KeyCodes.Space)
                {
                    Console.WriteLine($"[DEBUG] Restart key ({key}) pressed in Game Over - restarting game");
                    RestartGame();
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Key {key} pressed in Game Over but not restart key - ignoring");
                }
                break;
            case GameState.Playing:
                Console.WriteLine($"[DEBUG] Key {key} pressed during Playing - letting bird controller handle it");
                break;
        }
    }
    
    private void StartGame()
    {
        Console.WriteLine("[DEBUG] StartGame() called");
        Console.WriteLine($"[DEBUG] Pre-start state - Score: {score}, Game Time: {gameTime:F2}");
        
        SetGameState(GameState.Playing);
        score = 0;
        gameTime = 0.0f;
        lastScore = -1; // Reset score tracking
        
        Console.WriteLine($"[DEBUG] Game variables reset - Score: {score}, Game Time: {gameTime:F2}");
        
        // Reset bird position
        if (birdEntity != null)
        {
            var transform = birdEntity.GetComponent<TransformComponent>();
            var oldPosition = transform.Translation;
            transform.Translation = new Vector3(-2.0f, 0.0f, 0.0f);
            transform.Rotation = Vector3.Zero;
            
            Console.WriteLine($"[DEBUG] Bird position reset from {oldPosition} to {transform.Translation}");
            Console.WriteLine($"[DEBUG] Bird rotation reset to {transform.Rotation}");
        }
        else
        {
            Console.WriteLine("[DEBUG] ERROR: Cannot reset bird position - bird entity is null!");
        }
        
        // Enable pipe spawner
        EnablePipeSpawning(true);
        
        Console.WriteLine("Game Started!");
    }
    
    private void RestartGame()
    {
        Console.WriteLine("[DEBUG] RestartGame() called");
        
        // Count pipes before cleanup
        int pipeCount = CountPipes();
        Console.WriteLine($"[DEBUG] Found {pipeCount} pipes to clean up");
        
        // Clean up existing pipes
        CleanupAllPipes();
        
        // Verify cleanup
        int remainingPipes = CountPipes();
        Console.WriteLine($"[DEBUG] Pipe cleanup complete - {remainingPipes} pipes remaining");
        
        // Reset game state
        StartGame();
    }
    
    private void SetGameState(GameState newState)
    {
        GameState oldState = gameState;
        gameState = newState;
        
        Console.WriteLine($"[DEBUG] Game state transition: {oldState} -> {newState}");
        
        switch (newState)
        {
            case GameState.Menu:
                Console.WriteLine("[DEBUG] Entering Menu state - disabling pipe spawning");
                EnablePipeSpawning(false);
                break;
            case GameState.Playing:
                Console.WriteLine("[DEBUG] Entering Playing state - enabling pipe spawning");
                EnablePipeSpawning(true);
                break;
            case GameState.GameOver:
                Console.WriteLine("[DEBUG] Entering Game Over state - disabling pipe spawning");
                EnablePipeSpawning(false);
                Console.WriteLine($"Game Over! Final Score: {score}");
                break;
        }
    }
    
    private void CheckScoring()
    {
        if (birdEntity == null) 
        {
            if (enableDebugLogs)
                Console.WriteLine("[DEBUG] CheckScoring skipped - bird entity is null");
            return;
        }
        
        var birdPosition = birdEntity.GetComponent<TransformComponent>().Translation;
        int pipesChecked = 0;
        int scoringOpportunities = 0;
        
        // Check if bird passed through any pipes
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("BottomPipe"))
            {
                pipesChecked++;
                var pipeTransform = entity.GetComponent<TransformComponent>();
                var pipePosition = pipeTransform.Translation;
                
                // If bird X is just past the pipe X and we haven't scored this pipe yet
                if (birdPosition.X > pipePosition.X && pipePosition.X > birdPosition.X - 1.0f)
                {
                    scoringOpportunities++;
                    // Simple scoring - could be improved with better tracking
                    int oldScore = score;
                    score++;
                    Console.WriteLine($"[DEBUG] SCORE! Bird passed pipe at X:{pipePosition.X:F2} - Score: {oldScore} -> {score}");
                    Console.WriteLine($"Score: {score}");
                    // TODO: Play score sound effect
                }
            }
        }
        
        // Log score changes
        if (score != lastScore)
        {
            Console.WriteLine($"[DEBUG] Score changed from {lastScore} to {score}");
            lastScore = score;
        }
        
        if (enableDebugLogs && pipesChecked > 0)
        {
            Console.WriteLine($"[DEBUG] Scoring check - Bird X: {birdPosition.X:F2}, Pipes checked: {pipesChecked}, Scoring opportunities: {scoringOpportunities}");
        }
    }
    
    private bool IsBirdDead()
    {
        // This would be set by the bird controller when collision occurs
        // For now, just check Y bounds
        if (birdEntity == null) 
        {
            Console.WriteLine("[DEBUG] IsBirdDead check - bird entity is null, returning false");
            return false;
        }
        
        var position = birdEntity.GetComponent<TransformComponent>().Translation;
        bool outOfBounds = position.Y < -5.0f || position.Y > 10.0f;
        
        if (outOfBounds && enableDebugLogs)
        {
            Console.WriteLine($"[DEBUG] Bird death detected - Position Y: {position.Y:F2}, Bounds: [-5.0, 10.0]");
        }
        
        return outOfBounds;
    }
    
    private void EnablePipeSpawning(bool enable)
    {
        // Enable/disable pipe spawner script
        // This would require a way to enable/disable scripts
        Console.WriteLine($"[DEBUG] Pipe spawning: {(enable ? "Enabled" : "Disabled")}");
        
        if (pipeSpawnerEntity != null)
        {
            Console.WriteLine($"[DEBUG] Pipe spawner entity available for state change");
        }
        else
        {
            Console.WriteLine("[DEBUG] WARNING: Pipe spawner entity is null - cannot change state");
        }
    }
    
    private void CleanupAllPipes()
    {
        Console.WriteLine("[DEBUG] Starting pipe cleanup...");
        var pipesToRemove = new List<Entity>();
        
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                pipesToRemove.Add(entity);
                Console.WriteLine($"[DEBUG] Marked for removal: {entity.Name}");
            }
        }
        
        Console.WriteLine($"[DEBUG] Found {pipesToRemove.Count} pipes to remove");
        
        foreach (var pipe in pipesToRemove)
        {
            Console.WriteLine($"[DEBUG] Destroying pipe: {pipe.Name}");
            DestroyEntity(pipe);
        }
        
        Console.WriteLine($"[DEBUG] Pipe cleanup completed - {pipesToRemove.Count} pipes destroyed");
    }
    
    private int CountPipes()
    {
        int count = 0;
        foreach (var entity in CurrentScene.Entities)
        {
            if (entity.Name.Contains("Pipe"))
            {
                count++;
            }
        }
        return count;
    }
    
    private void UpdateUI()
    {
        // This would update UI elements with current score, game state, etc.
        // Requires UI system implementation
        if (enableDebugLogs && lastDebugLogTime == 0.0f && gameState == GameState.Playing)
        {
            Console.WriteLine($"[DEBUG] UI would show - Score: {score}, State: {gameState}");
        }
    }
    
    // Debug control methods
    public void SetDebugLogging(bool enabled)
    {
        enableDebugLogs = enabled;
        Console.WriteLine($"[DEBUG] Game Manager debug logging {(enabled ? "enabled" : "disabled")}");
    }
    
    public void SetDebugLogInterval(float interval)
    {
        debugLogInterval = interval;
        Console.WriteLine($"[DEBUG] Game Manager debug log interval set to {interval:F2} seconds");
    }
    
    // Public getters with debug info
    public int GetScore() 
    {
        if (enableDebugLogs)
            Console.WriteLine($"[DEBUG] Score requested: {score}");
        return score;
    }
    
    public GameState GetGameState() 
    {
        if (enableDebugLogs)
            Console.WriteLine($"[DEBUG] Game state requested: {gameState}");
        return gameState;
    }
}