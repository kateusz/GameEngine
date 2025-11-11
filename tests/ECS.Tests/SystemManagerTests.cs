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

    [Fact]
    public void RegisterSystem_AddsSystemToManager()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();

        // Act
        manager.RegisterSystem(system);

        // Assert
        Assert.Equal(1, manager.SystemCount);
    }

    [Fact]
    public void RegisterSystem_WithNull_ThrowsArgumentNullException()
    {
        // Arrange
        var manager = new SystemManager();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => manager.RegisterSystem(null!));
    }

    [Fact]
    public void RegisterSystem_WithDuplicateSystem_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();
        manager.RegisterSystem(system);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.RegisterSystem(system));
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
        Assert.True(system1.InitCalled);
        Assert.True(system2.InitCalled);
        Assert.True(manager.IsInitialized);
    }

    [Fact]
    public void Initialize_WhenCalledTwice_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        manager.Initialize();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.Initialize());
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
        Assert.True(system1.InitCalled);
        Assert.True(system2.InitCalled);
        Assert.True(system3.InitCalled);

        // Verify they were initialized in ascending priority order
        Assert.Equal(3, initOrder.Count);
        Assert.Equal(1, initOrder[0]); // System with priority 1 initialized first
        Assert.Equal(2, initOrder[1]); // System with priority 2 initialized second
        Assert.Equal(3, initOrder[2]); // System with priority 3 initialized third
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
        Assert.True(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
        Assert.Equal(deltaTime, system1.LastDeltaTime);
        Assert.Equal(deltaTime, system2.LastDeltaTime);
    }

    [Fact]
    public void Update_WithoutInitialize_ThrowsInvalidOperationException()
    {
        // Arrange
        var manager = new SystemManager();
        var system = new TestSystem();
        manager.RegisterSystem(system);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.Update(TimeSpan.Zero));
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
        Assert.True(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
        Assert.True(system3.UpdateCalled);

        // Verify they were called in ascending priority order
        Assert.Equal(3, updateOrder.Count);
        Assert.Equal(1, updateOrder[0]); // System with priority 1 called first
        Assert.Equal(2, updateOrder[1]); // System with priority 2 called second
        Assert.Equal(3, updateOrder[2]); // System with priority 3 called third
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
        Assert.True(system1.ShutdownCalled);
        Assert.True(system2.ShutdownCalled);
        Assert.Equal(0, manager.SystemCount);
        Assert.False(manager.IsInitialized);
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
        Assert.True(system1.ShutdownCalled);
        Assert.True(system2.ShutdownCalled);
        Assert.True(system3.ShutdownCalled);

        // Verify they were shut down in descending priority order (reverse of update order)
        Assert.Equal(3, shutdownOrder.Count);
        Assert.Equal(3, shutdownOrder[0]); // System with priority 3 shut down first
        Assert.Equal(2, shutdownOrder[1]); // System with priority 2 shut down second
        Assert.Equal(1, shutdownOrder[2]); // System with priority 1 shut down last
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
        Assert.Equal(new[] { "Init", "Update" }, system.CallOrder);
    }

    [Fact]
    public void IsInitialized_ReturnsFalseBeforeInitialize()
    {
        // Arrange
        var manager = new SystemManager();

        // Assert
        Assert.False(manager.IsInitialized);
    }

    [Fact]
    public void IsInitialized_ReturnsTrueAfterInitialize()
    {
        // Arrange
        var manager = new SystemManager();

        // Act
        manager.Initialize();

        // Assert
        Assert.True(manager.IsInitialized);
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
        Assert.False(manager.IsInitialized);
    }
}
