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
    private GameState _gameState = GameState.Menu;
    private int _score = 0;
    private float _gameTime = 0.0f;

    // Entity references
    private Entity _birdEntity;
    private Entity _pipeSpawnerEntity;

    // Component references
    private PipeSpawner _pipeSpawnerScript;
    private FlappyBirdController _birdScript;

    // Debug settings
    private float _debugLogInterval = 3.0f;
    private float _lastDebugLogTime = 0.0f;
    private int _lastScore = -1;
    private GameState _lastGameState = GameState.Menu;

    // Exposed fields
    public bool EnableDebugLogs = true;

    public override void OnCreate()
    {
        Console.WriteLine("[GameManager] ====== FLAPPY BIRD GAME MANAGER INITIALIZED ======");
        Console.WriteLine("[GameManager] Using SIMPLIFIED approach - bird stays fixed, pipes move!");

        // Find and connect to bird
        _birdEntity = FindEntity("Bird");
        if (_birdEntity != null)
        {
            Console.WriteLine($"[GameManager] Found bird entity: {_birdEntity.Name}");

            var birdScriptComponent = _birdEntity.GetComponent<NativeScriptComponent>();
            if (birdScriptComponent?.ScriptableEntity is FlappyBirdController bird)
            {
                _birdScript = bird;
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
        _pipeSpawnerEntity = FindEntity("PipeSpawner");
        if (_pipeSpawnerEntity != null)
        {
            Console.WriteLine($"[GameManager] Found pipe spawner entity: {_pipeSpawnerEntity.Name}");

            var spawnerScriptComponent = _pipeSpawnerEntity.GetComponent<NativeScriptComponent>();
            if (spawnerScriptComponent?.ScriptableEntity is PipeSpawner spawner)
            {
                _pipeSpawnerScript = spawner;
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

        if (_gameState == GameState.Playing)
        {
            _gameTime += deltaTime;

            // Check if bird died
            if (IsBirdDead())
            {
                TriggerGameOver();
            }
        }

        // Handle restart input during game over
        if (_gameState is GameState.GameOver or GameState.Menu)
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
        if (_gameState != GameState.Menu && _gameState != GameState.GameOver) return;

        Console.WriteLine("[GameManager] ====== STARTING NEW GAME ======");

        // Reset game state
        _score = 0;
        _gameTime = 0.0f;

        // Reset bird
        _birdScript.ResetBird();
        Console.WriteLine("[GameManager] Bird reset completed");
        

        // Clear any existing pipes
        _pipeSpawnerScript.DestroyAllPipes();
        Console.WriteLine("[GameManager] All pipes cleared");

        // Change to playing state
        SetGameState(GameState.Playing);

        Console.WriteLine("[GameManager] ====== GAME STARTED ======");
    }

    public void TriggerGameOver()
    {
        if (_gameState != GameState.Playing) return;

        Console.WriteLine("[GameManager] ====== GAME OVER TRIGGERED ======");
        Console.WriteLine($"[GameManager] Final Score: {_score}");
        Console.WriteLine($"[GameManager] Game Duration: {_gameTime:F2} seconds");

        SetGameState(GameState.GameOver);

        Console.WriteLine("[GameManager] Press R to restart");
    }

    public void IncrementScore()
    {
        int oldScore = _score;
        _score++;

        Console.WriteLine($"[GameManager] ====== SCORE! ======");
        Console.WriteLine($"[GameManager] {oldScore} -> {_score}");
        Console.WriteLine($"[GameManager] Game time: {_gameTime:F2}s");

        // Could trigger score sound effect here
        // AudioManager.PlayScoreSound();
    }

    private void RestartGame()
    {
        Console.WriteLine("[GameManager] ====== RESTARTING GAME ======");

        // Clean up pipes
        _pipeSpawnerScript.DestroyAllPipes();

        // Reset bird
        _birdScript.ResetBird();

        // Start new game
        StartGame();

        Console.WriteLine("[GameManager] ====== RESTART COMPLETE ======");
    }

    private void SetGameState(GameState newState)
    {
        GameState oldState = _gameState;
        _gameState = newState;

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
        if (_birdEntity == null || _birdScript == null)
        {
            return false;
        }

        return _birdScript.IsBirdDead();
    }

    private void PerformDebugLogging(float deltaTime)
    {
        _lastDebugLogTime += deltaTime;

        if (EnableDebugLogs && _lastDebugLogTime >= _debugLogInterval)
        {
            Console.WriteLine($"[GameManager] ====== DEBUG STATUS ======");
            Console.WriteLine($"[GameManager] State: {_gameState}");
            Console.WriteLine($"[GameManager] Score: {_score}");
            Console.WriteLine($"[GameManager] Game Time: {_gameTime:F2}s");

            if (_birdScript != null)
            {
                var birdPos = _birdScript.GetBirdPosition();
                Console.WriteLine(
                    $"[GameManager] Bird: ({birdPos.X:F2}, {birdPos.Y:F2}) - Dead: {_birdScript.IsBirdDead()}");
            }

            // Count pipes
            int pipeCount = 0;
            foreach (var entity in CurrentScene.Instance.Entities)
            {
                if (entity.Name.Contains("Pipe_"))
                {
                    pipeCount++;
                }
            }

            Console.WriteLine($"[GameManager] Active Pipes: {pipeCount}");

            Console.WriteLine($"[GameManager] ========================");
            _lastDebugLogTime = 0.0f;
        }

        // Log score changes immediately
        if (_score != _lastScore)
        {
            Console.WriteLine($"[GameManager] SCORE CHANGE: {_lastScore} -> {_score}");
            _lastScore = _score;
        }

        // Log state changes immediately
        if (_gameState != _lastGameState)
        {
            Console.WriteLine($"[GameManager] STATE CHANGE: {_lastGameState} -> {_gameState}");
            _lastGameState = _gameState;
        }
    }

    // Public getters for other scripts
    public GameState GetGameState()
    {
        return _gameState;
    }

    public int GetScore()
    {
        return _score;
    }

    public float GetGameTime()
    {
        return _gameTime;
    }

    public Vector3 GetBirdPosition()
    {
        return _birdScript?.GetBirdPosition() ?? Vector3.Zero;
    }
}