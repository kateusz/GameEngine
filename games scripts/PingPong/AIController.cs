using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class AIController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float MaxAiSpeed = 8.0f;
    public float Acceleration = 15.0f;
    public float ReactionTime = 0.1f;
    public float ErrorMargin = 0.2f;
    public float BoundaryTop = 4.0f;
    public float BoundaryBottom = -4.0f;
    public float PaddleWidth = 0.5f;
    public bool EnableAi = true;
    public float DifficultyLevel = 0.7f;
    
    // AI positioning and strategy
    public float DefensivePosition = 0.0f;
    public float MinimumMoveThreshold = 0.1f;
    
    // Update frequency control - THIS FIXES THE "TOO OFTEN" ISSUE
    public float AiDecisionInterval = 0.1f;  // How often AI makes decisions (seconds)
    public float BallSearchInterval = 0.5f;  // How often to search for ball if not found
    
    // Private fields
    private TransformComponent _transformComponent;
    private RigidBody2DComponent _rigidBodyComponent;
    private Vector3 _startPosition;
    private Entity _ballEntity;
    private BallController _ballController;
    
    // AI state
    private float _reactionTimer = 0f;
    private float _decisionTimer = 0f;        // NEW: Controls decision frequency
    private float _ballSearchTimer = 0f;      // NEW: Controls ball search frequency
    private Vector3 _targetPosition;
    private float _currentVelocityY = 0f;
    private bool _hasFoundBall = false;
    private bool _isIntercepting = false;
    private float _lastBallDirectionChange = 0f;
    private Vector2 _lastBallVelocity = Vector2.Zero;
    
    // AI error simulation
    private Random _random = new Random();
    private float _currentError = 0f;
    private float _errorUpdateTimer = 0f;
    
    // Performance monitoring
    private int _decisionsPerSecond = 0;
    private float _decisionCounter = 0f;
    private float _performanceTimer = 0f;
    
    public override void OnCreate()
    {
        Console.WriteLine("[AIController] Enhanced AI Paddle created");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[AIController] ERROR: No TransformComponent found!");
            return;
        }
        
        _transformComponent = GetComponent<TransformComponent>();
        _startPosition = _transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            _rigidBodyComponent = GetComponent<RigidBody2DComponent>();
        }
        
        // Try to find ball immediately
        FindBall();
        
        // Initialize target position
        _targetPosition = _startPosition;
        
        // Set difficulty-based decision frequency
        UpdateDecisionFrequency();
        
        Console.WriteLine($"[AIController] AI initialized at position: {_startPosition}");
        Console.WriteLine($"[AIController] Decision interval: {AiDecisionInterval:F3}s, Difficulty: {DifficultyLevel}");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        if (!EnableAi) return;
        
        float deltaTime = (float)ts.TotalSeconds;
        
        // Update timers
        _decisionTimer += deltaTime;
        _ballSearchTimer += deltaTime;
        _reactionTimer += deltaTime;
        _errorUpdateTimer += deltaTime;
        _performanceTimer += deltaTime;
        
        // Performance monitoring
        if (_performanceTimer >= 1.0f)
        {
            _decisionsPerSecond = (int)_decisionCounter;
            _decisionCounter = 0f;
            _performanceTimer = 0f;
            
            // Warn if making too many decisions
            if (_decisionsPerSecond > 15)
            {
                Console.WriteLine($"[AIController] WARNING: High decision rate: {_decisionsPerSecond}/sec");
            }
        }
        
        // CONTROLLED BALL SEARCHING - Only search every ballSearchInterval seconds
        if (!_hasFoundBall && _ballSearchTimer >= BallSearchInterval)
        {
            FindBall();
            _ballSearchTimer = 0f;
        }
        
        // CONTROLLED AI DECISIONS - Only make decisions every aiDecisionInterval seconds
        if (_hasFoundBall && _decisionTimer >= AiDecisionInterval && _reactionTimer >= ReactionTime)
        {
            MakeAiDecision();
            UpdateAiError(deltaTime);
            _decisionTimer = 0f;
            _reactionTimer = 0f;
            _decisionCounter++;
        }
        
        // MOVEMENT EXECUTION - This can happen every frame for smooth movement
        UpdateMovement(deltaTime);
        EnforceBoundaries();
    }
    
    private void UpdateDecisionFrequency()
    {
        // Adjust decision frequency based on difficulty
        // Easy AI: 5-8 decisions per second
        // Hard AI: 10-15 decisions per second
        float minFrequency = 5f;  // Hz
        float maxFrequency = 12f; // Hz
        
        float targetFrequency = Lerp(minFrequency, maxFrequency, DifficultyLevel);
        AiDecisionInterval = 1f / targetFrequency;
        
        Console.WriteLine($"[AIController] Decision frequency: {targetFrequency:F1} Hz ({AiDecisionInterval:F3}s interval)");
    }
    
    private void FindBall()
    {
        _ballEntity = FindEntity("Ball");
        if (_ballEntity != null)
        {
            var scriptComponent = _ballEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is BallController ball)
            {
                _ballController = ball;
                _hasFoundBall = true;
                Console.WriteLine("[AIController] Ball found and connected");
                return;
            }
        }
        
        // If we still haven't found it, increase search interval to avoid spam
        if (_ballSearchTimer > 2f)
        {
            BallSearchInterval = Math.Min(BallSearchInterval * 1.5f, 2f);
            Console.WriteLine($"[AIController] Ball not found, increasing search interval to {BallSearchInterval:F1}s");
        }
    }
    
    private void UpdateAiError(float deltaTime)
    {
        // Update error less frequently for better performance
        if (_errorUpdateTimer >= 0.8f) // Update every 0.8 seconds instead of 0.5
        {
            float maxError = ErrorMargin * (1.0f - DifficultyLevel);
            _currentError = (float)(_random.NextDouble() - 0.5) * 2.0f * maxError;
            _errorUpdateTimer = 0f;
        }
    }
    
    private void MakeAiDecision()
    {
        if (_ballController == null) return;
        
        var ballPos = _ballController.GetPosition();
        var ballVelocity = _ballController.GetVelocity();
        
        // Get fresh transform data
        _transformComponent = GetComponent<TransformComponent>();
        var currentPos = _transformComponent.Translation;
        
        // Only log important decisions to reduce console spam
        bool shouldLog = _isIntercepting != ShouldInterceptBall(ballPos, ballVelocity, currentPos);
        
        if (shouldLog)
        {
            Console.WriteLine($"[AIController] Ball pos: ({ballPos.X:F1}, {ballPos.Y:F1}), vel: ({ballVelocity.X:F1}, {ballVelocity.Y:F1})");
        }
        
        bool shouldIntercept = ShouldInterceptBall(ballPos, ballVelocity, currentPos);
        
        if (shouldIntercept)
        {
            // Calculate interception point with optimized prediction
            Vector3 interceptPoint = CalculateOptimizedInterceptionPoint(ballPos, ballVelocity, currentPos);
            _targetPosition = new Vector3(currentPos.X, interceptPoint.Y + _currentError, currentPos.Z);
            _isIntercepting = true;
            
            if (shouldLog)
            {
                Console.WriteLine($"[AIController] Intercepting at Y: {interceptPoint.Y:F2}");
            }
        }
        else
        {
            // Play defensively
            PlayDefensively(ballPos, ballVelocity, currentPos);
            _isIntercepting = false;
            
            if (shouldLog)
            {
                Console.WriteLine($"[AIController] Playing defensively, target Y: {DefensivePosition:F2}");
            }
        }
        
        // Clamp to boundaries
        _targetPosition = new Vector3(
            _targetPosition.X,
            Math.Clamp(_targetPosition.Y, BoundaryBottom, BoundaryTop),
            _targetPosition.Z
        );
    }
    
    private Vector3 CalculateOptimizedInterceptionPoint(Vector3 ballPos, Vector2 ballVelocity, Vector3 currentPos)
    {
        // Simplified calculation for better performance
        if (Math.Abs(ballVelocity.X) < 0.01f) 
        {
            return new Vector3(currentPos.X, ballPos.Y, currentPos.Z);
        }
        
        // Calculate time for ball to reach paddle's X position
        float timeToReach = Math.Abs((currentPos.X - ballPos.X) / ballVelocity.X);
        
        // Simple prediction with one bounce maximum (for performance)
        float predictedY = ballPos.Y + ballVelocity.Y * timeToReach;
        
        // Handle one wall bounce if needed
        if (predictedY > BoundaryTop)
        {
            float overshot = predictedY - BoundaryTop;
            predictedY = BoundaryTop - overshot;
        }
        else if (predictedY < BoundaryBottom)
        {
            float overshot = BoundaryBottom - predictedY;
            predictedY = BoundaryBottom + overshot;
        }
        
        return new Vector3(currentPos.X, predictedY, currentPos.Z);
    }
    
    private bool ShouldInterceptBall(Vector3 ballPos, Vector2 ballVelocity, Vector3 currentPos)
    {
        // Quick early exits for performance
        if (ballVelocity.Length() < 0.1f) return false;
        
        // Check if ball is moving towards AI paddle
        bool ballMovingTowardsAi = IsMovingTowardsAi(ballPos, ballVelocity, currentPos);
        
        if (!ballMovingTowardsAi) return false;
        
        // Simplified time calculation
        float timeToReachPaddle = Math.Abs((currentPos.X - ballPos.X) / ballVelocity.X);
        float distanceToMove = Math.Abs(ballPos.Y - currentPos.Y);
        float timeNeededToMove = distanceToMove / MaxAiSpeed;
        
        // Add difficulty-based margin
        float timeMargin = 0.3f * (1.0f - DifficultyLevel);
        
        return timeToReachPaddle > (timeNeededToMove + timeMargin);
    }
    
    private bool IsMovingTowardsAi(Vector3 ballPos, Vector2 ballVelocity, Vector3 aiPos)
    {
        if (aiPos.X > 0) // AI on right side
        {
            return ballVelocity.X > 0 && ballPos.X < aiPos.X - PaddleWidth;
        }
        else // AI on left side
        {
            return ballVelocity.X < 0 && ballPos.X > aiPos.X + PaddleWidth;
        }
    }
    
    private void PlayDefensively(Vector3 ballPos, Vector2 ballVelocity, Vector3 currentPos)
    {
        if (ballVelocity.Length() < 0.1f)
        {
            _targetPosition = new Vector3(currentPos.X, DefensivePosition, currentPos.Z);
            return;
        }
        
        // Simplified defensive positioning
        float strategicY = DefensivePosition;
        float ballDistance = Math.Abs(ballPos.X - currentPos.X);
        
        if (ballDistance > 3.0f)
        {
            strategicY = Lerp(DefensivePosition, ballPos.Y, 0.2f);
        }
        else
        {
            strategicY = Lerp(DefensivePosition, ballPos.Y, 0.4f);
        }
        
        _targetPosition = new Vector3(currentPos.X, strategicY, currentPos.Z);
    }
    
    private void UpdateMovement(float deltaTime)
    {
        // Get fresh component data
        _transformComponent = GetComponent<TransformComponent>();
        var currentPos = _transformComponent.Translation;
        float targetY = _targetPosition.Y;
        float currentY = currentPos.Y;
        float distance = targetY - currentY;
        
        // Don't move if we're close enough
        if (Math.Abs(distance) < MinimumMoveThreshold)
        {
            _currentVelocityY = 0f;
            return;
        }
        
        // Calculate desired velocity with difficulty scaling
        float desiredVelocity = Math.Sign(distance) * MaxAiSpeed * Lerp(0.6f, 1.0f, DifficultyLevel);
        
        // Apply smooth acceleration
        float velocityDiff = desiredVelocity - _currentVelocityY;
        float maxAccelChange = Acceleration * deltaTime;
        
        if (Math.Abs(velocityDiff) <= maxAccelChange)
        {
            _currentVelocityY = desiredVelocity;
        }
        else
        {
            _currentVelocityY += Math.Sign(velocityDiff) * maxAccelChange;
        }
        
        // Apply movement
        float moveAmount = _currentVelocityY * deltaTime;
        
        // Don't overshoot target
        if (Math.Abs(moveAmount) > Math.Abs(distance))
        {
            moveAmount = distance;
            _currentVelocityY = 0f;
        }
        
        // Apply movement to physics or transform
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            var body = _rigidBodyComponent.RuntimeBody;
            var currentPhysicsPos = body.GetPosition();
            var newY = Math.Clamp(currentPhysicsPos.Y + moveAmount, BoundaryBottom, BoundaryTop);
            
            body.SetTransform(new Vector2(currentPhysicsPos.X, newY), body.GetAngle());
            _transformComponent.Translation = new Vector3(currentPhysicsPos.X, newY, _transformComponent.Translation.Z);
            AddComponent(_transformComponent);
        }
        else
        {
            var newY = Math.Clamp(currentPos.Y + moveAmount, BoundaryBottom, BoundaryTop);
            _transformComponent.Translation = new Vector3(currentPos.X, newY, currentPos.Z);
            AddComponent(_transformComponent);
        }
    }
    
    private void EnforceBoundaries()
    {
        _transformComponent = GetComponent<TransformComponent>();
        var currentPos = _transformComponent.Translation;
        
        if (currentPos.Y > BoundaryTop || currentPos.Y < BoundaryBottom)
        {
            var clampedY = Math.Clamp(currentPos.Y, BoundaryBottom, BoundaryTop);
            
            _transformComponent.Translation = new Vector3(currentPos.X, clampedY, currentPos.Z);
            AddComponent(_transformComponent);
            
            if (_rigidBodyComponent?.RuntimeBody != null)
            {
                var body = _rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(currentPos.X, clampedY), body.GetAngle());
                body.SetLinearVelocity(Vector2.Zero);
            }
            
            _currentVelocityY = 0f;
        }
    }
    
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
    
    public void ResetPosition()
    {
        Console.WriteLine($"[AIController] Resetting AI paddle to start position: {_startPosition}");
        
        _transformComponent = new TransformComponent
        {
            Translation = _startPosition,
            Rotation = Vector3.Zero,
            Scale = Vector3.One
        };
        AddComponent(_transformComponent);
        
        if (_rigidBodyComponent?.RuntimeBody != null)
        {
            var body = _rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(_startPosition.X, _startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
        
        // Reset AI state and timers
        _reactionTimer = 0f;
        _decisionTimer = 0f;
        _ballSearchTimer = 0f;
        _currentVelocityY = 0f;
        _targetPosition = _startPosition;
        _isIntercepting = false;
        _lastBallVelocity = Vector2.Zero;
        _currentError = 0f;
        _decisionCounter = 0f;
        _decisionsPerSecond = 0;
    }
    
    public void SetDifficulty(float difficulty)
    {
        DifficultyLevel = Math.Clamp(difficulty, 0f, 1f);
        
        // Adjust parameters based on difficulty
        ReactionTime = Lerp(0.3f, 0.05f, difficulty);
        ErrorMargin = Lerp(0.8f, 0.1f, difficulty);
        MaxAiSpeed = Lerp(4f, 12f, difficulty);
        
        // Update decision frequency based on new difficulty
        UpdateDecisionFrequency();
        
        Console.WriteLine($"[AIController] Difficulty set to {difficulty:F2}");
        Console.WriteLine($"  - Reaction: {ReactionTime:F2}s, Error: {ErrorMargin:F2}, Speed: {MaxAiSpeed:F1}");
        Console.WriteLine($"  - Decision Rate: {(1f/AiDecisionInterval):F1} Hz");
    }
}