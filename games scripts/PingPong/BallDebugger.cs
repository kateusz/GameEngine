using System;
using System.Numerics;
using Engine.Scene;
using Engine.Scene.Components;

public class BallDebugger : ScriptableEntity
{
    private float _debugTimer = 0f;
    private const float DEBUG_INTERVAL = 0.5f; // Debug co pół sekundy
        
    public override void OnUpdate(TimeSpan ts)
    {
        _debugTimer += (float)ts.TotalSeconds;
            
        if (_debugTimer >= DEBUG_INTERVAL)
        {
            DebugPositions();
            _debugTimer = 0f;
        }
    }
        
    private void DebugPositions()
    {
        if (!HasComponent<TransformComponent>() || 
            !HasComponent<RigidBody2DComponent>() || 
            !HasComponent<BoxCollider2DComponent>())
        {
            Console.WriteLine("❌ Ball missing required components for debug!");
            return;
        }
            
        var transform = GetComponent<TransformComponent>();
        var rigidBody = GetComponent<RigidBody2DComponent>();
        var boxCollider = GetComponent<BoxCollider2DComponent>();
            
        Console.WriteLine($"🏀 === BALL POSITION DEBUG [{DateTime.Now:HH:mm:ss}] ===");
            
        // Transform info
        Console.WriteLine($"📍 Transform Position: ({transform.Translation.X:F2}, {transform.Translation.Y:F2})");
        Console.WriteLine($"📏 Transform Scale: ({transform.Scale.X:F2}, {transform.Scale.Y:F2})");
            
        // Physics body info
        if (rigidBody.RuntimeBody != null)
        {
            var bodyPos = rigidBody.RuntimeBody.GetPosition();
            Console.WriteLine($"🔵 Physics Body Position: ({bodyPos.X:F2}, {bodyPos.Y:F2})");
                
            // Sprawdź czy pozycje się różnią
            float posDiffX = Math.Abs(transform.Translation.X - bodyPos.X);
            float posDiffY = Math.Abs(transform.Translation.Y - bodyPos.Y);
                
            if (posDiffX > 0.01f || posDiffY > 0.01f)
            {
                Console.WriteLine($"⚠️  POSITION MISMATCH! Diff: ({posDiffX:F3}, {posDiffY:F3})");
            }
        }
        else
        {
            Console.WriteLine("❌ Physics Body is NULL!");
        }
            
        // Collider info
        Console.WriteLine($"📦 Collider Size: ({boxCollider.Size.X:F2}, {boxCollider.Size.Y:F2})");
        Console.WriteLine($"↔️  Collider Offset: ({boxCollider.Offset.X:F2}, {boxCollider.Offset.Y:F2})");
            
        // Obliczone wartości dla Box2D
        float actualSizeX = boxCollider.Size.X * transform.Scale.X;
        float actualSizeY = boxCollider.Size.Y * transform.Scale.Y;
        float actualOffsetX = boxCollider.Offset.X * transform.Scale.X;
        float actualOffsetY = boxCollider.Offset.Y * transform.Scale.Y;
            
        Console.WriteLine($"🔢 Calculated Size: ({actualSizeX:F2}, {actualSizeY:F2})");
        Console.WriteLine($"🔢 Calculated Offset: ({actualOffsetX:F2}, {actualOffsetY:F2})");
        Console.WriteLine($"✂️  Half-extents: ({actualSizeX/2f:F2}, {actualSizeY/2f:F2})");
            
        // Debug visualization size (zielony kwadrat)
        float debugVisSizeX = boxCollider.Size.X * 2.0f * transform.Scale.X;
        float debugVisSizeY = boxCollider.Size.Y * 2.0f * transform.Scale.Y;
        Console.WriteLine($"👁️  Debug Vis Size: ({debugVisSizeX:F2}, {debugVisSizeY:F2})");
            
        // Sprawdź czy sprite i collider mają podobny rozmiar
        if (HasComponent<SpriteRendererComponent>())
        {
            var sprite = GetComponent<SpriteRendererComponent>();
            if (sprite.Texture != null)
            {
                // Rozmiar sprite'a w world units (z uwzględnieniem scale)
                float spriteWorldSizeX = (sprite.Texture.Width / 100f) * transform.Scale.X; // Assuming 100 pixels per unit
                float spriteWorldSizeY = (sprite.Texture.Height / 100f) * transform.Scale.Y;
                    
                Console.WriteLine($"🖼️  Sprite Texture: {sprite.Texture.Width}x{sprite.Texture.Height}");
                Console.WriteLine($"🖼️  Sprite World Size: ({spriteWorldSizeX:F2}, {spriteWorldSizeY:F2})");
                    
                // Porównaj z rozmiarem collidera
                float sizeDiffX = Math.Abs(spriteWorldSizeX - actualSizeX);
                float sizeDiffY = Math.Abs(spriteWorldSizeY - actualSizeY);
                    
                if (sizeDiffX > 0.1f || sizeDiffY > 0.1f)
                {
                    Console.WriteLine($"⚠️  SIZE MISMATCH! Sprite vs Collider diff: ({sizeDiffX:F2}, {sizeDiffY:F2})");
                }
            }
        }
            
        Console.WriteLine($"🏀 ============================================");
        Console.WriteLine();
    }
}