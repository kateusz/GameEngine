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

    // Entity references
    private Entity birdEntity;
    private Entity pipeSpawnerEntity;

    // Component references
    private PipeSpawner pipeSpawnerScript;
    private FlappyBirdController birdScript;

    // Debug settings
    private float debugLogInterval = 3.0f;
    private float lastDebugLogTime = 0.0f;
    private int lastScore = -1;
    private GameState lastGameState = GameState.Menu;

    // Exposed fields
    public bool enableDebugLogs = true;

    public override void OnCreate()
    {
        Console.WriteLine("[GameManager] ====== FLAPPY BIRD GAME MANAGER INITIALIZED ======");
        Console.WriteLine("[GameManager] Using SIMPLIFIED approach - bird stays fixed, pipes move!");

        // Find and connect to bird
        birdEntity = FindEntity("Bird");
        if (birdEntity != null)
        {
            Console.WriteLine($"[GameManager] Found bird entity: {birdEntity.Name}");

            var birdScriptComponent = birdEntity.GetComponent<NativeScriptComponent>();
            if (birdScriptComponent?.ScriptableEntity is FlappyBirdController bird)
            {
                birdScript = bird;
                Console.WriteLine("[GameManager] Connected to FlappyBirdController");
            }
            else
            {
                Console.WriteLine("[GameManager] WARNING: Bird entity found but no FlappyBirdController script");
            }
        }
        else
        {
            Console.WriteLine("[GameManager] ERROR: Bird entity not found!");
        }

        // Find and connect to pipe spawner
        pipeSpawnerEntity = FindEntity("PipeSpawner");
        if (pipeSpawnerEntity != null)
        {
            Console.WriteLine($"[GameManager] Found pipe spawner entity: {pipeSpawnerEntity.Name}");

            var spawnerScriptComponent = pipeSpawnerEntity.GetComponent<NativeScriptComponent>();
            if (spawnerScriptComponent?.ScriptableEntity is PipeSpawner spawner)
            {
                pipeSpawnerScript = spawner;
                Console.WriteLine("[GameManager] Connected to PipeSpawner");
            }
            else
            {
                Console.WriteLine("[GameManager] WARNING: Pipe spawner entity found but no PipeSpawner script");
            }
        }
        else
        {
            Console.WriteLine("[GameManager] ERROR: Pipe spawner entity not found!");
        }

        // Start in menu state
        SetGameState(GameState.Menu);
        Console.WriteLine("[GameManager] ====== INITIALIZATION COMPLETE ======");
    }

    public override void OnUpdate(TimeSpan ts)
    {
        float deltaTime = (float)ts.TotalSeconds;

        if (gameState == GameState.Playing)
        {
            gameTime += deltaTime;

            // Check if bird died
            if (IsBirdDead())
            {
                TriggerGameOver();
            }
        }

        // Handle restart input during game over
        if (gameState is GameState.GameOver or GameState.Menu)
        {
            if (InputState.Instance.Keyboard.IsKeyPressed(KeyCodes.R))
            {
                RestartGame();
            }
        }

        // Debug logging
        PerformDebugLogging(deltaTime);
    }

    // Public methods for other scripts to call
    public void StartGame()
    {
        if (gameState != GameState.Menu && gameState != GameState.GameOver) return;

        Console.WriteLine("[GameManager] ====== STARTING NEW GAME ======");

        // Reset game state
        score = 0;
        gameTime = 0.0f;

        // Reset bird
        birdScript.ResetBird();
        Console.WriteLine("[GameManager] Bird reset completed");
        

        // Clear any existing pipes
        pipeSpawnerScript.DestroyAllPipes();
        Console.WriteLine("[GameManager] All pipes cleared");

        // Change to playing state
        SetGameState(GameState.Playing);

        Console.WriteLine("[GameManager] ====== GAME STARTED ======");
    }

    public void TriggerGameOver()
    {
        if (gameState != GameState.Playing) return;

        Console.WriteLine("[GameManager] ====== GAME OVER TRIGGERED ======");
        Console.WriteLine($"[GameManager] Final Score: {score}");
        Console.WriteLine($"[GameManager] Game Duration: {gameTime:F2} seconds");

        SetGameState(GameState.GameOver);

        Console.WriteLine("[GameManager] Press R to restart");
    }

    public void IncrementScore()
    {
        int oldScore = score;
        score++;

        Console.WriteLine($"[GameManager] ====== SCORE! ======");
        Console.WriteLine($"[GameManager] {oldScore} -> {score}");
        Console.WriteLine($"[GameManager] Game time: {gameTime:F2}s");

        // Could trigger score sound effect here
        // AudioManager.PlayScoreSound();
    }

    private void RestartGame()
    {
        Console.WriteLine("[GameManager] ====== RESTARTING GAME ======");

        // Clean up pipes
        pipeSpawnerScript.DestroyAllPipes();

        // Reset bird
        birdScript.ResetBird();

        // Start new game
        StartGame();

        Console.WriteLine("[GameManager] ====== RESTART COMPLETE ======");
    }

    private void SetGameState(GameState newState)
    {
        GameState oldState = gameState;
        gameState = newState;

        Console.WriteLine($"[GameManager] ====== STATE CHANGE: {oldState} -> {newState} ======");

        switch (newState)
        {
            case GameState.Menu:
                Console.WriteLine("[GameManager] MENU STATE:");
                Console.WriteLine("[GameManager] - Press Space or click to start");
                Console.WriteLine("[GameManager] - Bird can jump but game won't start until StartGame() called");
                break;

            case GameState.Playing:
                Console.WriteLine("[GameManager] PLAYING STATE:");
                Console.WriteLine("[GameManager] - Bird responds to input");
                Console.WriteLine("[GameManager] - Pipes spawn and move");
                Console.WriteLine("[GameManager] - Collision detection active");
                Console.WriteLine("[GameManager] - Score tracking active");
                break;

            case GameState.GameOver:
                Console.WriteLine("[GameManager] GAME OVER STATE:");
                Console.WriteLine("[GameManager] - All movement stopped");
                Console.WriteLine("[GameManager] - Bird input disabled");
                Console.WriteLine("[GameManager] - Press R to restart");
                break;
        }
    }

    private bool IsBirdDead()
    {
        if (birdEntity == null || birdScript == null)
        {
            return false;
        }

        return birdScript.IsBirdDead();
    }

    private void PerformDebugLogging(float deltaTime)
    {
        lastDebugLogTime += deltaTime;

        if (enableDebugLogs && lastDebugLogTime >= debugLogInterval)
        {
            Console.WriteLine($"[GameManager] ====== DEBUG STATUS ======");
            Console.WriteLine($"[GameManager] State: {gameState}");
            Console.WriteLine($"[GameManager] Score: {score}");
            Console.WriteLine($"[GameManager] Game Time: {gameTime:F2}s");

            if (birdScript != null)
            {
                var birdPos = birdScript.GetBirdPosition();
                Console.WriteLine(
                    $"[GameManager] Bird: ({birdPos.X:F2}, {birdPos.Y:F2}) - Dead: {birdScript.IsBirdDead()}");
            }

            // Count pipes
            int pipeCount = 0;
            foreach (var entity in CurrentScene.Entities)
            {
                if (entity.Name.Contains("Pipe_"))
                {
                    pipeCount++;
                }
            }

            Console.WriteLine($"[GameManager] Active Pipes: {pipeCount}");

            Console.WriteLine($"[GameManager] ========================");
            lastDebugLogTime = 0.0f;
        }

        // Log score changes immediately
        if (score != lastScore)
        {
            Console.WriteLine($"[GameManager] SCORE CHANGE: {lastScore} -> {score}");
            lastScore = score;
        }

        // Log state changes immediately
        if (gameState != lastGameState)
        {
            Console.WriteLine($"[GameManager] STATE CHANGE: {lastGameState} -> {gameState}");
            lastGameState = gameState;
        }
    }

    // Public getters for other scripts
    public GameState GetGameState()
    {
        return gameState;
    }

    public int GetScore()
    {
        return score;
    }

    public float GetGameTime()
    {
        return gameTime;
    }

    public Vector3 GetBirdPosition()
    {
        return birdScript?.GetBirdPosition() ?? Vector3.Zero;
    }
}