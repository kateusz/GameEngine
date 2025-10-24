# Physics Benchmark Design Analysis

## Overview

This document provides a comprehensive analysis and design for implementing physics benchmarks in the Benchmark project. The goal is to create realistic, measurable physics simulations that can track performance metrics for the Box2D integration.

---

## Current State Analysis

### Existing Benchmark Infrastructure

The Benchmark project currently has:
- **BenchmarkLayer**: Main benchmarking UI and orchestration
- **BenchmarkTestType**: Enum defining test types
- **BenchmarkResult**: Data structure for storing test results
- **BenchmarkStorage**: Persistence for baseline comparisons
- **Existing Tests**: Renderer2D stress, texture switching, draw call optimization

**Strengths:**
- Well-structured test framework with configurable parameters
- Real-time performance monitoring with frame time graphs
- Baseline comparison system for regression testing
- Custom metrics support per test type

**Current Focus:**
- Primarily rendering/graphics benchmarks
- No physics-specific tests

### Physics System Capabilities

The engine uses Box2D.NetStandard with the following components:

**RigidBody2DComponent:**
- Body types: Static, Dynamic, Kinematic
- Fixed rotation support
- Integrates with Box2D Body objects

**BoxCollider2DComponent:**
- Box-shaped collision geometry
- Properties: Size, Offset, Density, Friction, Restitution
- Trigger support

**Physics Simulation:**
- Fixed timestep: 60 Hz (1/60s per step)
- Accumulator pattern to prevent frame-rate dependent physics
- Configurable velocity/position iterations
- Safety limits to prevent spiral of death
- Gravity: (0, -9.8) m/s²

---

## Proposed Physics Benchmarks

### 1. Falling Bodies Stress Test

**Objective:** Measure performance with many dynamic bodies under gravity

**Scenario:**
- Spawn N dynamic rigid bodies (configurable: 100-10,000)
- Bodies start at varying heights above ground
- Static ground collider at bottom
- Let bodies fall, collide, and settle

**Metrics:**
- Average FPS during simulation
- Frame time during collision-heavy period (first 2-3 seconds)
- Number of Box2D body updates per frame
- Number of active collisions per frame
- Time to simulation stability (all bodies sleeping)

**Physics Parameters:**
```csharp
// Dynamic bodies
Density: 1.0f
Friction: 0.3f
Restitution: 0.0f (no bounce for this test)
Size: Random(0.2f - 1.0f)

// Static ground
Width: 50.0f
Height: 1.0f
Position: (0, -10)
```

**Expected Performance Characteristics:**
- Initial spawn: lightweight
- First 2-3s: Heavy (many collisions)
- After settling: Light (bodies asleep)

---

### 2. Bouncing Ball Test

**Objective:** Measure physics accuracy and performance with high restitution

**Scenario:**
- Single or multiple balls dropped from height
- High restitution for multiple bounces
- Static floor with restitution
- Track bounce height degradation

**Metrics:**
- Frame time consistency
- Bounce height accuracy (energy conservation)
- Number of bounces before settling
- Contact point precision
- FPS during continuous bouncing

**Physics Parameters:**
```csharp
// Ball
Density: 2.0f
Friction: 0.1f
Restitution: 0.8f - 0.95f (high bounce)
Radius: 0.5f

// Floor
Restitution: 0.8f
Friction: 0.5f
```

**Variations:**
- Single ball: Physics accuracy benchmark
- 100 balls: Performance benchmark
- 1000 balls: Stress test

---

### 3. Stacking Stability Test

**Objective:** Test solver quality with stacked objects

**Scenario:**
- Stack N boxes on top of each other
- Static ground at bottom
- Measure stability and jitter

**Metrics:**
- Average position deviation per body
- Frame time with complex constraint solving
- Stack collapse time (if unstable)
- Solver iteration efficiency

**Physics Parameters:**
```csharp
// Boxes
Density: 1.0f
Friction: 0.6f (high for stability)
Restitution: 0.0f
Size: (1.0f, 1.0f)
Spacing: 1.05f (slight gap to test settling)

Stack heights: 5, 10, 20, 50
```

---

### 4. Collision Pairs Stress Test

**Objective:** Measure broad-phase and narrow-phase collision performance

**Scenario:**
- Create dense grid of dynamic bodies in confined space
- All bodies colliding with many neighbors
- Measure collision detection overhead

**Metrics:**
- FPS with high collision pair count
- Active collision count
- Broad-phase efficiency
- Memory allocations during collisions

**Physics Parameters:**
```csharp
// Bodies in 20x20 grid
Count: 400
Size: 0.4f x 0.4f
Spacing: 0.45f (overlap slightly)
Density: 1.0f

// Container walls (4 static bodies)
```

---

### 5. Mixed Body Types Test

**Objective:** Test performance with diverse physics scenarios

**Scenario:**
- Combination of static, dynamic, and kinematic bodies
- Moving platforms (kinematic)
- Static obstacles
- Dynamic falling objects

**Metrics:**
- FPS with mixed body types
- Kinematic body update overhead
- Static body optimization verification

**Physics Parameters:**
```csharp
Static bodies: 20 (obstacles)
Dynamic bodies: 500 (falling/moving)
Kinematic bodies: 5 (moving platforms)
```

---

### 6. Continuous Spawning Test

**Objective:** Measure physics performance with constant entity creation/destruction

**Scenario:**
- Spawn new dynamic bodies at regular intervals
- Bodies fall off screen and get destroyed
- Maintains constant body count

**Metrics:**
- Frame time consistency during spawn/destroy
- Memory allocation patterns
- Physics world cleanup efficiency

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. **Extend BenchmarkTestType enum:**
```csharp
public enum BenchmarkTestType
{
    None,
    Renderer2DStress,
    TextureSwitching,
    DrawCallOptimization,

    // New physics tests
    PhysicsFallingBodies,
    PhysicsBouncingBall,
    PhysicsStacking,
    PhysicsCollisionPairs,
    PhysicsMixedBodies,
    PhysicsContinuousSpawn
}
```

2. **Add Physics-Specific Metrics to BenchmarkResult:**
```csharp
// In CustomMetrics dictionary:
- "Active Body Count"
- "Sleeping Body Count"
- "Active Collision Count"
- "Avg Velocity Magnitude"
- "Physics Step Count"
- "Time to Stability (s)"
```

3. **Create Physics Test Scene Setup Methods:**
- `SetupPhysicsFallingBodiesTest()`
- `SetupPhysicsBouncingBallTest()`
- etc.

4. **Create Physics Test Update Methods:**
- `UpdatePhysicsFallingBodies()`
- Track custom physics metrics
- Handle entity lifecycle

### Phase 2: Test Implementation

**Recommended Starting Point: Bouncing Ball Test**

**Rationale:**
- Simple to implement and visualize
- Easily scalable (1 ball → 1000 balls)
- Tests both accuracy and performance
- Good for baseline establishment

**Implementation Steps:**

```csharp
private void SetupPhysicsBouncingBallTest()
{
    if (_currentTestScene == null) return;

    // Create static floor
    var floor = _currentTestScene.CreateEntity("Floor");
    floor.AddComponent<TransformComponent>();
    var floorTransform = floor.GetComponent<TransformComponent>();
    floorTransform.Translation = new Vector3(0, -5, 0);
    floorTransform.Scale = new Vector3(20, 1, 1);

    floor.AddComponent<RigidBody2DComponent>().BodyType = RigidBodyType.Static;
    floor.AddComponent<BoxCollider2DComponent>(new BoxCollider2DComponent
    {
        Size = new Vector2(1.0f, 1.0f),
        Density = 1.0f,
        Friction = 0.5f,
        Restitution = 0.8f  // Floor bounces too
    });

    // Optionally add SpriteRendererComponent for visualization
    floor.AddComponent<SpriteRendererComponent>();
    var floorSprite = floor.GetComponent<SpriteRendererComponent>();
    floorSprite.Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);

    // Create bouncing ball(s)
    var random = new Random();
    for (int i = 0; i < _entityCount; i++)
    {
        var ball = _currentTestScene.CreateEntity($"Ball_{i}");
        ball.AddComponent<TransformComponent>();
        var ballTransform = ball.GetComponent<TransformComponent>();

        // Spawn at random X positions, high Y
        ballTransform.Translation = new Vector3(
            (float)(random.NextDouble() * 10 - 5),
            5.0f + i * 0.5f,
            0
        );
        ballTransform.Scale = Vector3.One * 0.5f;

        var rb = ball.AddComponent<RigidBody2DComponent>();
        rb.BodyType = RigidBodyType.Dynamic;

        var collider = ball.AddComponent<BoxCollider2DComponent>();
        collider.Size = new Vector2(1.0f, 1.0f);
        collider.Density = 2.0f;
        collider.Friction = 0.1f;
        collider.Restitution = 0.85f;  // High bounce

        // Visual
        var sprite = ball.AddComponent<SpriteRendererComponent>();
        sprite.Color = new Vector4(
            (float)random.NextDouble(),
            (float)random.NextDouble(),
            (float)random.NextDouble(),
            1.0f
        );
    }

    // Initialize physics world for benchmark
    _currentTestScene.OnRuntimeStart();
}
```

**Update Method:**
```csharp
private void UpdatePhysicsBouncingBall()
{
    if (_currentTestScene == null) return;

    // Track physics-specific metrics
    int activeBodyCount = 0;
    int sleepingBodyCount = 0;
    float totalVelocity = 0f;

    var view = Context.Instance.View<RigidBody2DComponent>();
    foreach (var (entity, component) in view)
    {
        if (component.RuntimeBody != null)
        {
            if (component.RuntimeBody.IsAwake())
            {
                activeBodyCount++;
                var velocity = component.RuntimeBody.GetLinearVelocity();
                totalVelocity += new Vector2(velocity.X, velocity.Y).Length();
            }
            else
            {
                sleepingBodyCount++;
            }
        }
    }

    // Store metrics for final report
    // (Would need to track these in class fields for averaging)
    _currentPhysicsMetrics.ActiveBodyCount = activeBodyCount;
    _currentPhysicsMetrics.SleepingBodyCount = sleepingBodyCount;
    _currentPhysicsMetrics.AverageVelocity = activeBodyCount > 0
        ? totalVelocity / activeBodyCount
        : 0f;
}
```

### Phase 3: UI and Configuration

**Add Physics Test Controls to BenchmarkUI:**
```csharp
private void RenderBenchmarkUI()
{
    // ... existing code ...

    ImGui.Separator();
    ImGui.Text("Physics Tests:");

    if (ImGui.Button("Falling Bodies Stress"))
        StartBenchmark(BenchmarkTestType.PhysicsFallingBodies);

    if (ImGui.Button("Bouncing Ball Test"))
        StartBenchmark(BenchmarkTestType.PhysicsBouncingBall);

    if (ImGui.Button("Stacking Stability"))
        StartBenchmark(BenchmarkTestType.PhysicsStacking);

    // ... etc ...
}
```

**Add Physics-Specific Configuration:**
```csharp
// Physics test parameters
private float _gravity = -9.8f;
private int _physicsIterationsVelocity = 6;
private int _physicsIterationsPosition = 2;
private bool _allowSleeping = true;

// In UI:
ImGui.DragFloat("Gravity", ref _gravity, 0.1f, -20.0f, 0.0f);
ImGui.DragInt("Velocity Iterations", ref _physicsIterationsVelocity, 1, 1, 20);
ImGui.DragInt("Position Iterations", ref _physicsIterationsPosition, 1, 1, 20);
ImGui.Checkbox("Allow Body Sleeping", ref _allowSleeping);
```

### Phase 4: Results and Metrics

**Enhanced Results Display for Physics:**
```csharp
private void RenderResultsWindow()
{
    // ... existing code ...

    // Physics-specific metrics
    if (result.TestName.StartsWith("Physics"))
    {
        ImGui.Text("Physics Metrics:");
        ImGui.Indent();

        if (result.CustomMetrics.ContainsKey("Active Body Count"))
            ImGui.Text($"Avg Active Bodies: {result.CustomMetrics["Active Body Count"]}");

        if (result.CustomMetrics.ContainsKey("Active Collision Count"))
            ImGui.Text($"Avg Collisions: {result.CustomMetrics["Active Collision Count"]}");

        if (result.CustomMetrics.ContainsKey("Time to Stability (s)"))
            ImGui.Text($"Stability Time: {result.CustomMetrics["Time to Stability (s)"]}s");

        ImGui.Unindent();
    }
}
```

---

## Physics Test Matrix

| Test Name | Primary Metric | Entity Count | Complexity | Expected FPS* |
|-----------|----------------|--------------|------------|---------------|
| Bouncing Ball (Single) | Accuracy | 1 | Low | 1000+ |
| Bouncing Ball (100) | Performance | 100 | Medium | 200-500 |
| Bouncing Ball (1000) | Stress | 1000 | High | 60-120 |
| Falling Bodies (500) | Collision | 500 | Medium | 120-240 |
| Falling Bodies (5000) | Stress | 5000 | High | 30-60 |
| Stacking (10) | Stability | 11 | Medium | 500+ |
| Stacking (50) | Solver | 51 | High | 200-400 |
| Collision Pairs | Broad-phase | 400 | Very High | 60-120 |
| Mixed Bodies | Real-world | 525 | Medium | 150-300 |
| Continuous Spawn | Lifecycle | 100-200 | Medium | 180-300 |

*Expected FPS on modern hardware (M1/M2 Mac, recent Intel/AMD)

---

## Performance Optimization Opportunities

Based on benchmark results, the following optimizations may be identified:

1. **Spatial Partitioning**: If broad-phase collision detection shows up in profiling
2. **Body Sleeping Optimization**: Tune sleeping thresholds for better performance
3. **Collision Caching**: Implement persistent contact caching if not already present
4. **Island Management**: Optimize constraint island detection
5. **Timestep Tuning**: Adjust fixed timestep based on performance requirements

---

## Success Criteria

A successful physics benchmark implementation should:

1. ✓ Support at least 3 different physics test scenarios
2. ✓ Track physics-specific metrics (active bodies, collisions, etc.)
3. ✓ Integrate seamlessly with existing benchmark infrastructure
4. ✓ Provide baseline comparison for regression detection
5. ✓ Scale from accuracy tests (few entities) to stress tests (thousands)
6. ✓ Visualize physics simulation in real-time
7. ✓ Export results for performance tracking over time

---

## Recommended Implementation Order

1. **Week 1**: Bouncing Ball Test (single + multi)
   - Simple, visual, scalable
   - Establishes physics benchmark patterns

2. **Week 2**: Falling Bodies Stress Test
   - More complex collision scenarios
   - Performance metrics collection

3. **Week 3**: Stacking Stability Test
   - Solver quality testing
   - Stability metrics

4. **Week 4**: Advanced Tests (Collision Pairs, Mixed Bodies, Continuous Spawn)
   - Specialized scenarios
   - Edge case coverage

---

## Technical Considerations

### Scene Lifecycle Management

Physics tests need special handling:
```csharp
private void StartBenchmark(BenchmarkTestType testType)
{
    // ... existing code ...

    // For physics tests, initialize physics world
    if (IsPhysicsTest(testType))
    {
        _currentTestScene.OnRuntimeStart();
    }
}

private void StopBenchmark()
{
    // ... existing code ...

    // Cleanup physics world
    if (_currentTestScene != null && IsPhysicsTest(_currentTestType))
    {
        _currentTestScene.OnRuntimeStop();
    }
}
```

### Timestep Handling

Physics tests use fixed timestep, so need to call the physics update:
```csharp
private void UpdateBenchmark(TimeSpan ts)
{
    // ... existing code ...

    if (IsPhysicsTest(_currentTestType))
    {
        // This calls the fixed timestep physics simulation
        _currentTestScene.OnUpdateRuntime(ts);
    }
}
```

### Visualization

Physics tests should visualize:
- Bodies with sprite renderers
- Optional: Physics debug overlay (collider bounds)
- Color-coding for body types (static=green, dynamic=red, kinematic=blue)

---

## Conclusion

The bouncing ball test is the **recommended starting point** because:
- Simple implementation
- Visual feedback makes debugging easy
- Easily scalable from accuracy test to stress test
- Tests fundamental physics: gravity, collisions, restitution
- Good baseline for comparing future optimizations

The falling bodies test is a close second and could be implemented alongside or immediately after the bouncing ball test.

Both tests provide valuable performance data and will integrate cleanly with the existing benchmark infrastructure.