using System;
using System.IO;
using System.Numerics;
using ECS;
using Editor;
using Engine.Platform.SilkNet.Audio;
using Engine.Scene;
using Engine.Scene.Components;

public class BallController : ScriptableEntity
{
    // Public fields (editable in editor)
    public float initialSpeed = 0.5f;
    public float maxSpeed = 2.0f;
    public float speedIncrease = 0.1f; // Speed increase per paddle hit
    
    public float boundaryTop = 4.5f;
    public float boundaryBottom = -4.5f;
    public float boundaryLeft = -8.0f;
    public float boundaryRight = 8.0f;
    
    // Private fields
    private TransformComponent transformComponent;
    private RigidBody2DComponent rigidBodyComponent;
    private Vector3 startPosition;
    private Vector2 velocity;
    private float currentSpeed;
    private bool isActive = false;
    
    // Game Manager reference
    private Entity gameManagerEntity;
    private PingPongGameManager gameManager;
    private Random random = new Random();
    
    public override void OnCreate()
    {
        Console.WriteLine("[BallController] Ball created");
        
        // Get required components
        if (!HasComponent<TransformComponent>())
        {
            Console.WriteLine("[BallController] ERROR: No TransformComponent found!");
            return;
        }
        
        transformComponent = GetComponent<TransformComponent>();
        startPosition = transformComponent.Translation;
        
        // Get physics component if available
        if (HasComponent<RigidBody2DComponent>())
        {
            rigidBodyComponent = GetComponent<RigidBody2DComponent>();
            Console.WriteLine("[BallController] Physics body found");
        }
        
        // Find game manager
        gameManagerEntity = FindEntity("Game Manager");
        if (gameManagerEntity != null)
        {
            var scriptComponent = gameManagerEntity.GetComponent<NativeScriptComponent>();
            if (scriptComponent?.ScriptableEntity is PingPongGameManager manager)
            {
                gameManager = manager;
                Console.WriteLine("[BallController] Connected to Game Manager");
            }
        }
        
        currentSpeed = initialSpeed;
        Console.WriteLine($"[BallController] Ball initialized at position: {startPosition}");
    }
    
    public override void OnUpdate(TimeSpan ts)
    {
        if (!isActive) return;
        
        float deltaTime = (float)ts.TotalSeconds;
        
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            // Update using physics body
            UpdatePhysicsMovement(deltaTime);
        }
        else
        {
            // Update using manual movement
            UpdateManualMovement(deltaTime);
        }
        
        CheckBoundaryCollisions();
    }
    
    private void UpdatePhysicsMovement(float deltaTime)
    {
        var body = rigidBodyComponent.RuntimeBody;
        var currentVel = body.GetLinearVelocity();
        
        // Maintain consistent speed
        if (currentVel.Length() > 0.1f)
        {
            var normalizedVel = Vector2.Normalize(currentVel);
            body.SetLinearVelocity(normalizedVel * currentSpeed);
        }
    }
    
    private void UpdateManualMovement(float deltaTime)
    {
        var currentPos = transformComponent.Translation;
        var newPos = currentPos + new Vector3(velocity.X * deltaTime, velocity.Y * deltaTime, 0);
        
        transformComponent.Translation = newPos;
        SetComponent(transformComponent);
    }
    
    private void CheckBoundaryCollisions()
    {
        var body = rigidBodyComponent.RuntimeBody;
        var currentPos = body.GetPosition();
        bool bounced = false;
        
        // Top and bottom boundaries
        if (currentPos.Y > boundaryTop)
        {
            transformComponent.Translation = new Vector3(currentPos.X, boundaryTop, 0);
            velocity = new Vector2(velocity.X, -Math.Abs(velocity.Y)); // Bounce down
            bounced = true;
        }
        else if (currentPos.Y < boundaryBottom)
        {
            transformComponent.Translation = new Vector3(currentPos.X, boundaryBottom,0);
            velocity = new Vector2(velocity.X, Math.Abs(velocity.Y)); // Bounce up
            bounced = true;
        }
        
        // Left and right boundaries (scoring)
        if (currentPos.X < boundaryLeft)
        {
            // Player 2 scores
            OnScore(false);
        }
        else if (currentPos.X > boundaryRight)
        {
            // Player 1 scores
            OnScore(true);
        }
        
        SetComponent(transformComponent);
        
        if (bounced)
        {
            // Update physics body if present
            if (rigidBodyComponent?.RuntimeBody != null)
            {
                body.SetTransform(new Vector2(transformComponent.Translation.X, transformComponent.Translation.Y), 0);
                body.SetLinearVelocity(velocity);
            }
        }
    }
    
    public override void OnCollisionBegin(Entity other)
    {
        // Check if we hit a paddle
        if (other.Name.Contains("Paddle"))
        {
            OnPaddleHit(other);
        }
    }
    
    private void OnPaddleHit(Entity paddle)
    {
        Console.WriteLine($"[BallController] Ball hit paddle: {paddle.Name}");
        
        var paddlePos = paddle.GetComponent<TransformComponent>().Translation;
        var ballPos = transformComponent.Translation;
        
        // Calculate bounce angle based on where the ball hit the paddle
        float paddleHeight = paddle.GetComponent<TransformComponent>().Scale.Y;
        float hitPoint = (ballPos.Y - paddlePos.Y) / paddleHeight; // -0.5 to 0.5
        
        // Clamp hit point
        hitPoint = Math.Clamp(hitPoint, -0.5f, 0.5f);
        
        // Calculate new velocity
        float bounceAngle = hitPoint * 60.0f; // Max 60 degrees
        float bounceAngleRad = bounceAngle * (float)Math.PI / 180.0f;
        
        // Determine direction based on which paddle was hit
        bool isLeftPaddle = paddlePos.X < 0;
        float xDirection = isLeftPaddle ? 1.0f : -1.0f;
        
        // Increase speed
        currentSpeed = Math.Min(currentSpeed + speedIncrease, maxSpeed);
        
        // Set new velocity
        velocity = new Vector2(
            xDirection * currentSpeed * (float)Math.Cos(bounceAngleRad),
            currentSpeed * (float)Math.Sin(bounceAngleRad)
        );
        
        // Update physics body if present
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            rigidBodyComponent.RuntimeBody.SetLinearVelocity(velocity);
        }
        
        Console.WriteLine($"[BallController] New velocity: ({velocity.X:F2}, {velocity.Y:F2}), Speed: {currentSpeed:F2}");
        AudioEngine.Instance.PlayOneShot(Path.Combine(AssetsManager.AssetsPath, "audio/swing.wav"), 0.5f);
    }
    
    private void OnScore(bool player1Scored)
    {
        Console.WriteLine($"[BallController] Score! Player {(player1Scored ? "1" : "2")} scored");
        
        isActive = false;
        
        // Notify game manager
        if (gameManager != null)
        {
            if (player1Scored)
                gameManager.Player1Score();
            else
                gameManager.Player2Score();
        }
    }
    
    public void Launch(bool towardsPlayer1 = true)
    {
        Console.WriteLine($"[BallController] Launching ball towards Player {(towardsPlayer1 ? "1" : "2")}");
        
        isActive = true;
        currentSpeed = initialSpeed;
        
        // Random angle between -30 and 30 degrees
        float angle = (float)(random.NextDouble() * 60.0 - 30.0) * (float)Math.PI / 180.0f;
        float xDirection = towardsPlayer1 ? -1.0f : 1.0f;
        
        velocity = new Vector2(
            xDirection * currentSpeed * (float)Math.Cos(angle),
            currentSpeed * (float)Math.Sin(angle)
        );
        
        // Update physics body if present
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            rigidBodyComponent.RuntimeBody.SetLinearVelocity(velocity);
        }
        
        Console.WriteLine($"[BallController] Launch velocity: ({velocity.X:F2}, {velocity.Y:F2})");
    }
    
    public void ResetBall()
    {
        Console.WriteLine("[BallController] Resetting ball");
        
        isActive = false;
        velocity = Vector2.Zero;
        currentSpeed = initialSpeed;
        
        transformComponent.Translation = startPosition;
        SetComponent(transformComponent);
        
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            var body = rigidBodyComponent.RuntimeBody;
            body.SetTransform(new Vector2(startPosition.X, startPosition.Y), 0);
            body.SetLinearVelocity(Vector2.Zero);
        }
    }
    
    public void Stop()
    {
        isActive = false;
        velocity = Vector2.Zero;
        
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            rigidBodyComponent.RuntimeBody.SetLinearVelocity(Vector2.Zero);
        }
    }
    
    // Public getters
    public Vector3 GetPosition()
    {
        return transformComponent.Translation;
    }
    
    public Vector2 GetVelocity()
    {
        if (rigidBodyComponent?.RuntimeBody != null)
        {
            return rigidBodyComponent.RuntimeBody.GetLinearVelocity();
        }
        return velocity;
    }
    
    public float GetSpeed()
    {
        return currentSpeed;
    }
    
    public bool IsActive()
    {
        return isActive;
    }
}