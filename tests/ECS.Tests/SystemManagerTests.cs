using ECS.Systems;
using Shouldly;

namespace ECS.Tests;

public class SystemManagerTests
{
    // Test system implementation for testing purposes
    private class TestSystem : ISystem
    {
        public int Priority { get; set; }
        public bool InitCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool ShutdownCalled { get; private set; }
        public TimeSpan LastDeltaTime { get; private set; }
        public List<string> CallOrder { get; } = new();
        public List<int>? SharedInitOrder { get; set; } // Track init call order across systems
        public List<int>? SharedUpdateOrder { get; set; } // Track update call order across systems
        public List<int>? SharedShutdownOrder { get; set; } // Track shutdown call order across systems

        public void OnInit()
        {
            InitCalled = true;
            CallOrder.Add("Init");
            SharedInitOrder?.Add(Priority); // Record when this system was initialized
        }

        public void OnUpdate(TimeSpan deltaTime)
        {
            UpdateCalled = true;
            LastDeltaTime = deltaTime;
            CallOrder.Add("Update");
            SharedUpdateOrder?.Add(Priority); // Record when this system was called
        }

        public void OnShutdown()
        {
            ShutdownCalled = true;
            CallOrder.Add("Shutdown");
            SharedShutdownOrder?.Add(Priority); // Record when this system was shut down
        }
    }

    private sealed class CountingShutdownSystem : ISystem
    {
        public int Priority => 0;
        public int ShutdownCount { get; private set; }

        public void OnInit() { }
        public void OnUpdate(TimeSpan deltaTime) { }
        public void OnShutdown() => ShutdownCount++;
    }

    [Fact]
    public void RegisterSystem_AddsSystemToManager()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();

        // Act
        manager.RegisterSystem(system);

        // Assert
        manager.SystemCount.ShouldBe(1);
    }

    [Fact]
    public void RegisterSystem_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new SystemManager();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => manager.RegisterSystem(null!));
    }

    [Fact]
    public void RegisterSystem_WithDuplicateSystem_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();
        manager.RegisterSystem(system);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => manager.RegisterSystem(system));
    }

    [Fact]
    public void Initialize_CallsOnInitOnAllSystems()
    {
        // Arrange
        var manager = new SystemManager();
        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);

        // Act
        manager.Initialize();

        // Assert
        system1.InitCalled.ShouldBeTrue();
        system2.InitCalled.ShouldBeTrue();
        manager.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Initialize_WhenCalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        manager.Initialize();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => manager.Initialize());
    }

    [Fact]
    public void Initialize_CallsSystemsInPriorityOrder()
    {
        // Arrange
        var manager = new SystemManager();
        var initOrder = new List<int>();

        var system3 = new TestSystem { Priority = 3, SharedInitOrder = initOrder };
        var system1 = new TestSystem { Priority = 1, SharedInitOrder = initOrder };
        var system2 = new TestSystem { Priority = 2, SharedInitOrder = initOrder };

        // Register in non-priority order
        manager.RegisterSystem(system3);
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);

        // Act
        manager.Initialize();

        // Assert - All systems should have been initialized in priority order (1, 2, 3)
        system1.InitCalled.ShouldBeTrue();
        system2.InitCalled.ShouldBeTrue();
        system3.InitCalled.ShouldBeTrue();

        // Verify they were initialized in ascending priority order
        initOrder.Count.ShouldBe(3);
        initOrder[0].ShouldBe(1); // System with priority 1 initialized first
        initOrder[1].ShouldBe(2); // System with priority 2 initialized second
        initOrder[2].ShouldBe(3); // System with priority 3 initialized third
    }

    [Fact]
    public void Update_CallsOnUpdateOnAllSystems()
    {
        // Arrange
        var manager = new SystemManager();
        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.Initialize();

        var deltaTime = TimeSpan.FromSeconds(0.016);

        // Act
        manager.Update(deltaTime);

        // Assert
        system1.UpdateCalled.ShouldBeTrue();
        system2.UpdateCalled.ShouldBeTrue();
        system1.LastDeltaTime.ShouldBe(deltaTime);
        system2.LastDeltaTime.ShouldBe(deltaTime);
    }

    [Fact]
    public void Update_WithoutInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();
        manager.RegisterSystem(system);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => manager.Update(TimeSpan.Zero));
    }

    [Fact]
    public void Update_CallsSystemsInPriorityOrder()
    {
        // Arrange
        var manager = new SystemManager();
        var updateOrder = new List<int>();

        var system3 = new TestSystem { Priority = 3, SharedUpdateOrder = updateOrder };
        var system1 = new TestSystem { Priority = 1, SharedUpdateOrder = updateOrder };
        var system2 = new TestSystem { Priority = 2, SharedUpdateOrder = updateOrder };

        // Register in non-priority order
        manager.RegisterSystem(system3);
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.Initialize();

        // Act
        manager.Update(TimeSpan.FromSeconds(0.016));

        // Assert - All systems should have been called in priority order (1, 2, 3)
        system1.UpdateCalled.ShouldBeTrue();
        system2.UpdateCalled.ShouldBeTrue();
        system3.UpdateCalled.ShouldBeTrue();

        // Verify they were called in ascending priority order
        updateOrder.Count.ShouldBe(3);
        updateOrder[0].ShouldBe(1); // System with priority 1 called first
        updateOrder[1].ShouldBe(2); // System with priority 2 called second
        updateOrder[2].ShouldBe(3); // System with priority 3 called third
    }

    [Fact]
    public void Shutdown_CalledTwice_OnlyInvokesOnShutdownOnce()
    {
        var manager = new SystemManager();
        var system = new CountingShutdownSystem();
        manager.RegisterSystem(system);
        manager.Initialize();

        manager.Shutdown();
        manager.Shutdown();

        system.ShutdownCount.ShouldBe(1);
    }

    [Fact]
    public void Shutdown_CallsOnShutdownButKeepsSystemsRegistered()
    {
        var manager = new SystemManager();
        var system = new TestSystem { Priority = 1 };
        manager.RegisterSystem(system);
        manager.Initialize();

        manager.Shutdown();

        system.ShutdownCalled.ShouldBeTrue();
        manager.SystemCount.ShouldBe(1);
        manager.IsInitialized.ShouldBeFalse();

        manager.Initialize();
        manager.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Shutdown_CallsOnShutdownOnAllSystems()
    {
        // Arrange
        var manager = new SystemManager();
        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.Initialize();

        // Act
        manager.Shutdown();

        // Assert
        system1.ShutdownCalled.ShouldBeTrue();
        system2.ShutdownCalled.ShouldBeTrue();
        manager.SystemCount.ShouldBe(2);
        manager.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void Shutdown_CallsSystemsInReverseOrder()
    {
        // Arrange
        var manager = new SystemManager();
        var shutdownOrder = new List<int>();

        var system1 = new TestSystem { Priority = 1, SharedShutdownOrder = shutdownOrder };
        var system2 = new TestSystem { Priority = 2, SharedShutdownOrder = shutdownOrder };
        var system3 = new TestSystem { Priority = 3, SharedShutdownOrder = shutdownOrder };

        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.RegisterSystem(system3);
        manager.Initialize();

        // Act
        manager.Shutdown();

        // Assert - All systems should have been shut down in reverse priority order (3, 2, 1)
        system1.ShutdownCalled.ShouldBeTrue();
        system2.ShutdownCalled.ShouldBeTrue();
        system3.ShutdownCalled.ShouldBeTrue();

        // Verify they were shut down in descending priority order (reverse of update order)
        shutdownOrder.Count.ShouldBe(3);
        shutdownOrder[0].ShouldBe(3); // System with priority 3 shut down first
        shutdownOrder[1].ShouldBe(2); // System with priority 2 shut down second
        shutdownOrder[2].ShouldBe(1); // System with priority 1 shut down last
    }

    [Fact]
    public void SystemLifecycle_OnInitCalledBeforeFirstUpdate()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();
        manager.RegisterSystem(system);

        // Act
        manager.Initialize();
        manager.Update(TimeSpan.FromSeconds(0.016));

        // Assert
        system.CallOrder.ShouldBe(new[] { "Init", "Update" });
    }

    [Fact]
    public void Dispose_DoesNotDisposeSharedSystems()
    {
        var manager = new SystemManager();
        var shared = new DisposableTestSystem { Priority = 1 };
        var perScene = new DisposableTestSystem { Priority = 2 };
        manager.RegisterSystem(shared, isShared: true);
        manager.RegisterSystem(perScene);

        manager.Dispose();

        shared.DisposeCalled.ShouldBeFalse();
        perScene.DisposeCalled.ShouldBeTrue();
    }

    private sealed class DisposableTestSystem : ISystem, IDisposable
    {
        public int Priority { get; set; }
        public bool DisposeCalled { get; private set; }

        public void OnInit() { }
        public void OnUpdate(TimeSpan deltaTime) { }
        public void OnShutdown() { }
        public void Dispose() => DisposeCalled = true;
    }

    [Fact]
    public void IsInitialized_ReturnsFalseBeforeInitialize()
    {
        // Arrange
        var manager = new SystemManager();

        // Assert
        manager.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void IsInitialized_ReturnsTrueAfterInitialize()
    {
        // Arrange
        var manager = new SystemManager();

        // Act
        manager.Initialize();

        // Assert
        manager.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void IsInitialized_ReturnsFalseAfterShutdown()
    {
        // Arrange
        var manager = new SystemManager();
        manager.Initialize();

        // Act
        manager.Shutdown();

        // Assert
        manager.IsInitialized.ShouldBeFalse();
    }
}
