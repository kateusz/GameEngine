using System;
using System.Numerics;
using ECS;
using Engine.Scene;
using Engine.Scene.Components;

public class AIController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float maxAISpeed = 8.0f;
    public float acceleration = 15.0f;
    public float reactionTime = 0.1f;
    public float errorMargin = 0.2f;
    public float boundaryTop = 4.0f;
    public float boundaryBottom = -4.0f;
    public float paddleWidth = 0.5f;
    public bool enableAI = true;
    public float difficultyLevel = 0.7f;
    
    // AI positioning and strategy
    public float defensivePosition = 0.0f;
    public float minimumMoveThreshold = 0.1f;
    
    // Update frequency control - THIS FIXES THE "TOO OFTEN" ISSUE
    public float aiDecisionInterval = 0.1f;  // How often AI makes decisions (seconds)
    public float ballSearchInterval = 0.5f;  // How often to search for ball if not found
    
    // Private fields
    private TransformComponent transformComponent;
    private RigidBody2DComponent rigidBodyComponent;
    private Vector3 startPosition;
    private Entity ballEntity;
    private BallController ballController;
    
    // AI state
    private float reactionTimer = 0f;
    private float decisionTimer = 0f;        // NEW: Controls decision frequency
    private float ballSearchTimer = 0f;      // NEW: Controls ball search frequency
    private Vector3 targetPosition;
    private float currentVelocityY = 0f;
    private bool hasFoundBall = false;
    private bool isIntercepting = false;
    private float lastBallDirectionChange = 0f;
    private Vector2 lastBallVelocity = Vector2.Zero;
    
    // AI error simulation
    private Random random = new Random();
    private float currentError = 0f;
    private float errorUpdateTimer = 0f;
    
    // Performance monitoring
    private int decisionsPerSecond = 0;
    private float decisionCounter = 0f;
    private float performanceTimer = 0f;
    
    public override void OnCreate()
    {
        Console.WriteLine("[AIController] Enhanced AI Paddle created");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[AIController] ERROR: No TransformComponent found!");
            return;
        }
        
        transformComponent = GetComponent<TransformComponent>();
        startPosition = transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            rigidBodyComponent = GetComponent<RigidBody2DComponent>();
        }
        
        // Try to find ball immediately
        FindBall();
        
        // Initialize target position
        targetPosition = startPosition;
        
        // Set difficulty-based decision frequency
        UpdateDecisionFrequency();
        
        Console.WriteLine($"[AIController] AI initialized at position: {startPosition}");
        Console.WriteLine($"[AIController] Decision interval: {aiDecisionInterval:F3}s, Difficulty: {difficultyLevel}");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        if (!enableAI) return;
        
        float deltaTime = (float)ts.TotalSeconds;
        
        // Update timers
        decisionTimer += deltaTime;
        ballSearchTimer += deltaTime;
        reactionTimer += deltaTime;
        errorUpdateTimer += deltaTime;
        performanceTimer += deltaTime;
        
        // Performance monitoring
        if (performanceTimer >= 1.0f)
        {
            decisionsPerSecond = (int)decisionCounter;
            decisionCounter = 0f;
            performanceTimer = 0f;
            
            // Warn if making too many decisions
            if (decisionsPerSecond > 15)
            {
                Console.WriteLine($"[AIController] WARNING: High decision rate: {decisionsPerSecond}/sec");
            }
        }
        
        // CONTROLLED BALL SEARCHING - Only search every ballSearchInterval seconds
        if (!hasFoundBall && ballSearchTimer >= ballSearchInterval)
        {
            FindBall();
            ballSearchTimer = 0f;
        }
        
        // CONTROLLED AI DECISIONS - Only make decisions every aiDecisionInterval seconds
        if (hasFoundBall && decisionTimer >= aiDecisionInterval && reactionTimer >= reactionTime)
        {
            MakeAIDecision();
            UpdateAIError(deltaTime);
            decisionTimer = 0f;
            reactionTimer = 0f;
            decisionCounter++;
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
        
        float targetFrequency = Lerp(minFrequency, maxFrequency, difficultyLevel);
        aiDecisionInterval = 1f / targetFrequency;
        
        Console.WriteLine($"[AIController] Decision frequency: {targetFrequency:F1} Hz ({aiDecisionInterval:F3}s interval)");
    }
    
    private void FindBall()
    {
        ballEntity = FindEntity("Ball");
        if (ballEntity != null)
        {
            var scriptComponent = ballEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is BallController ball)
            {
                ballController = ball;
                hasFoundBall = true;
                Console.WriteLine("[AIController] Ball found and connected");
                return;
            }
        }
        
        // If we still haven't found it, increase search interval to avoid spam
        if (ballSearchTimer > 2f)
        {
            ballSearchInterval = Math.Min(ballSearchInterval * 1.5f, 2f);
            Console.WriteLine($"[AIController] Ball not found, increasing search interval to {ballSearchInterval:F1}s");
        }
    }
    
    private void UpdateAIError(float deltaTime)
    {
        // Update error less frequently for better performance
        if (errorUpdateTimer >= 0.8f) // Update every 0.8 seconds instead of 0.5
        {
            float maxError = errorMargin * (1.0f - difficultyLevel);
            currentError = (float)(random.NextDouble() - 0.5) * 2.0f * maxError;
            errorUpdateTimer = 0f;
        }
    }
    
    private void MakeAIDecision()
    {
        if (ballController == null) return;
        
        var ballPos = ballController.GetPosition();
        var ballVelocity = ballController.GetVelocity();
        
        // Get fresh transform data
        transformComponent = GetComponent<TransformComponent>();
        var currentPos = transformComponent.Translation;
        
        // Only log important decisions to reduce console spam
        bool shouldLog = isIntercepting != ShouldInterceptBall(ballPos, ballVelocity, currentPos);
        
        if (shouldLog)
        {
            Console.WriteLine($"[AIController] Ball pos: ({ballPos.X:F1}, {ballPos.Y:F1}), vel: ({ballVelocity.X:F1}, {ballVelocity.Y:F1})");
        }
        
        bool shouldIntercept = ShouldInterceptBall(ballPos, ballVelocity, currentPos);
        
        if (shouldIntercept)
        {
            // Calculate interception point with optimized prediction
            Vector3 interceptPoint = CalculateOptimizedInterceptionPoint(ballPos, ballVelocity, currentPos);
            targetPosition = new Vector3(currentPos.X, interceptPoint.Y + currentError, currentPos.Z);
            isIntercepting = true;
            
            if (shouldLog)
            {
                Console.WriteLine($"[AIController] Intercepting at Y: {interceptPoint.Y:F2}");
            }
        }
        else
        {
            // Play defensively
            PlayDefensively(ballPos, ballVelocity, currentPos);
            isIntercepting = false;
            
            if (shouldLog)
            {
                Console.WriteLine($"[AIController] Playing defensively, target Y: {defensivePosition:F2}");
            }
        }
        
        // Clamp to boundaries
        targetPosition = new Vector3(
            targetPosition.X,
            Math.Clamp(targetPosition.Y, boundaryBottom, boundaryTop),
            targetPosition.Z
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
        if (predictedY > boundaryTop)
        {
            float overshot = predictedY - boundaryTop;
            predictedY = boundaryTop - overshot;
        }
        else if (predictedY < boundaryBottom)
        {
            float overshot = boundaryBottom - predictedY;
            predictedY = boundaryBottom + overshot;
        }
        
        return new Vector3(currentPos.X, predictedY, currentPos.Z);
    }
    
    private bool ShouldInterceptBall(Vector3 ballPos, Vector2 ballVelocity, Vector3 currentPos)
    {
        // Quick early exits for performance
        if (ballVelocity.Length() < 0.1f) return false;
        
        // Check if ball is moving towards AI paddle
        bool ballMovingTowardsAI = IsMovingTowardsAI(ballPos, ballVelocity, currentPos);
        
        if (!ballMovingTowardsAI) return false;
        
        // Simplified time calculation
        float timeToReachPaddle = Math.Abs((currentPos.X - ballPos.X) / ballVelocity.X);
        float distanceToMove = Math.Abs(ballPos.Y - currentPos.Y);
        float timeNeededToMove = distanceToMove / maxAISpeed;
        
        // Add difficulty-based margin
        float timeMargin = 0.3f * (1.0f - difficultyLevel);
        
        return timeToReachPaddle > (timeNeededToMove + timeMargin);
    }
    
    private bool IsMovingTowardsAI(Vector3 ballPos, Vector2 ballVelocity, Vector3 aiPos)
    {
        if (aiPos.X > 0) // AI on right side
        {
            return ballVelocity.X > 0 && ballPos.X < aiPos.X - paddleWidth;
        }
        else // AI on left side
        {
            return ballVelocity.X < 0 && ballPos.X > aiPos.X + paddleWidth;
        }
    }
    
    private void PlayDefensively(Vector3 ballPos, Vector2 ballVelocity, Vector3 currentPos)
    {
        if (ballVelocity.Length() < 0.1f)
        {
            targetPosition = new Vector3(currentPos.X, defensivePosition, currentPos.Z);
            return;
        }
        
        // Simplified defensive positioning
        float strategicY = defensivePosition;
        float ballDistance = Math.Abs(ballPos.X - currentPos.X);
        
        if (ballDistance > 3.0f)
        {
            strategicY = Lerp(defensivePosition, ballPos.Y, 0.2f);
        }
        else
        {
            strategicY = Lerp(defensivePosition, ballPos.Y, 0.4f);
        }
        
        targetPosition = new Vector3(currentPos.X, strategicY, currentPos.Z);
    }
    
    private void UpdateMovement(float deltaTime)
    {
        // Get fresh component data
        transformComponent = GetComponent<TransformComponent>();
        var currentPos = transformComponent.Translation;
        float targetY = targetPosition.Y;
        float currentY = currentPos.Y;
        float distance = targetY - currentY;
        
        // Don't move if we're close enough
        if (Math.Abs(distance) < minimumMoveThreshold)
        {
            currentVelocityY = 0f;
            return;
        }
        
        // Calculate desired velocity with difficulty scaling
        float desiredVelocity = Math.Sign(distance) * maxAISpeed * Lerp(0.6f, 1.0f, difficultyLevel);
        
        // Apply smooth acceleration
        float velocityDiff = desiredVelocity - currentVelocityY;
        float maxAccelChange = acceleration * deltaTime;
        
        if (Math.Abs(velocityDiff) <= maxAccelChange)
        {
            currentVelocityY = desiredVelocity;
        }
        else
        {
            currentVelocityY += Math.Sign(velocityDiff) * maxAccelChange;
        }
        
        // Apply movement
        float moveAmount = currentVelocityY * deltaTime;
        
        // Don't overshoot target
        if (Math.Abs(moveAmount) > Math.Abs(distance))
        {
            moveAmount = distance;
            currentVelocityY = 0f;
        }
        
        // Apply movement to physics or transform
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            var body = rigidBodyComponent.RuntimeBody;
            var currentPhysicsPos = body.GetPosition();
            var newY = Math.Clamp(currentPhysicsPos.Y + moveAmount, boundaryBottom, boundaryTop);
            
            body.SetTransform(new Vector2(currentPhysicsPos.X, newY), body.GetAngle());
            transformComponent.Translation = new Vector3(currentPhysicsPos.X, newY, transformComponent.Translation.Z);
            AddComponent(transformComponent);
        }
        else
        {
            var newY = Math.Clamp(currentPos.Y + moveAmount, boundaryBottom, boundaryTop);
            transformComponent.Translation = new Vector3(currentPos.X, newY, currentPos.Z);
            AddComponent(transformComponent);
        }
    }
    
    private void EnforceBoundaries()
    {
        transformComponent = GetComponent<TransformComponent>();
        var currentPos = transformComponent.Translation;
        
        if (currentPos.Y > boundaryTop || currentPos.Y < boundaryBottom)
        {
            var clampedY = Math.Clamp(currentPos.Y, boundaryBottom, boundaryTop);
            
            transformComponent.Translation = new Vector3(currentPos.X, clampedY, currentPos.Z);
            AddComponent(transformComponent);
            
            if (rigidBodyComponent?.RuntimeBody != null)
            {
                var body = rigidBodyComponent.RuntimeBody;
                body.SetTransform(new Vector2(currentPos.X, clampedY), body.GetAngle());
                body.SetLinearVelocity(Vector2.Zero);
            }
            
            currentVelocityY = 0f;
        }
    }
    
    private float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
    
    public void ResetPosition()
    {
        Console.WriteLine($"[AIController] Resetting AI paddle to start position: {startPosition}");
        
        transformComponent = new TransformComponent
        {
            Translation = startPosition,
            Rotation = Vector3.Zero,
            Scale = Vector3.One
        };
        AddComponent(transformComponent);
        
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            var body = rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(startPosition.X, startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
        
        // Reset AI state and timers
        reactionTimer = 0f;
        decisionTimer = 0f;
        ballSearchTimer = 0f;
        currentVelocityY = 0f;
        targetPosition = startPosition;
        isIntercepting = false;
        lastBallVelocity = Vector2.Zero;
        currentError = 0f;
        decisionCounter = 0f;
        decisionsPerSecond = 0;
    }
    
    public void SetDifficulty(float difficulty)
    {
        difficultyLevel = Math.Clamp(difficulty, 0f, 1f);
        
        // Adjust parameters based on difficulty
        reactionTime = Lerp(0.3f, 0.05f, difficulty);
        errorMargin = Lerp(0.8f, 0.1f, difficulty);
        maxAISpeed = Lerp(4f, 12f, difficulty);
        
        // Update decision frequency based on new difficulty
        UpdateDecisionFrequency();
        
        Console.WriteLine($"[AIController] Difficulty set to {difficulty:F2}");
        Console.WriteLine($"  - Reaction: {reactionTime:F2}s, Error: {errorMargin:F2}, Speed: {maxAISpeed:F1}");
        Console.WriteLine($"  - Decision Rate: {(1f/aiDecisionInterval):F1} Hz");
    }
}