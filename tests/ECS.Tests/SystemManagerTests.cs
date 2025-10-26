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

        public void OnInit()
        {
            InitCalled = true;
            CallOrder.Add("Init");
        }

        public void OnUpdate(TimeSpan deltaTime)
        {
            UpdateCalled = true;
            LastDeltaTime = deltaTime;
            CallOrder.Add("Update");
        }

        public void OnShutdown()
        {
            ShutdownCalled = true;
            CallOrder.Add("Shutdown");
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
    public void RegisterSystem_AfterInitialize_CallsOnInitImmediately()
    {
        // Arrange
        var manager = new SystemManager();
        manager.Initialize();
        var system = new TestSystem();

        // Act
        manager.RegisterSystem(system);

        // Assert
        Assert.True(system.InitCalled);
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
        var callOrder = new List<int>();

        var system3 = new TestSystem { Priority = 3 };
        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };

        // Register in non-priority order
        manager.RegisterSystem(system3);
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);

        // Track call order
        system1.CallOrder.Clear();
        system2.CallOrder.Clear();
        system3.CallOrder.Clear();

        var initOrder = new List<TestSystem>();
        system1.CallOrder.Add("Init");
        system2.CallOrder.Add("Init");
        system3.CallOrder.Add("Init");

        // Act
        manager.Initialize();

        // Verify that systems were called
        Assert.True(system1.InitCalled);
        Assert.True(system2.InitCalled);
        Assert.True(system3.InitCalled);
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

        var system3 = new TestSystem { Priority = 3 };
        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };

        // Register in non-priority order
        manager.RegisterSystem(system3);
        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.Initialize();

        // Act
        manager.Update(TimeSpan.FromSeconds(0.016));

        // Assert - All systems should have been called
        Assert.True(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
        Assert.True(system3.UpdateCalled);
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

        var system1 = new TestSystem { Priority = 1 };
        var system2 = new TestSystem { Priority = 2 };
        var system3 = new TestSystem { Priority = 3 };

        manager.RegisterSystem(system1);
        manager.RegisterSystem(system2);
        manager.RegisterSystem(system3);
        manager.Initialize();

        // Act
        manager.Shutdown();

        // Assert - All systems should have been shut down
        Assert.True(system1.ShutdownCalled);
        Assert.True(system2.ShutdownCalled);
        Assert.True(system3.ShutdownCalled);
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
    public void PriorityOrdering_SystemsExecuteInAscendingPriorityOrder()
    {
        // Arrange
        var manager = new SystemManager();
        var executionOrder = new List<int>();

        var system10 = new TestSystem { Priority = 10 };
        var system5 = new TestSystem { Priority = 5 };
        var system1 = new TestSystem { Priority = 1 };
        var system15 = new TestSystem { Priority = 15 };

        // Register in random order
        manager.RegisterSystem(system10);
        manager.RegisterSystem(system5);
        manager.RegisterSystem(system15);
        manager.RegisterSystem(system1);

        manager.Initialize();

        // Act
        manager.Update(TimeSpan.FromSeconds(0.016));

        // Assert - Verify all systems were called
        Assert.True(system1.UpdateCalled);
        Assert.True(system5.UpdateCalled);
        Assert.True(system10.UpdateCalled);
        Assert.True(system15.UpdateCalled);
    }

    [Fact]
    public void PriorityOrdering_NegativePrioritiesExecuteFirst()
    {
        // Arrange
        var manager = new SystemManager();

        var systemPositive = new TestSystem { Priority = 10 };
        var systemNegative = new TestSystem { Priority = -5 };
        var systemZero = new TestSystem { Priority = 0 };

        manager.RegisterSystem(systemPositive);
        manager.RegisterSystem(systemNegative);
        manager.RegisterSystem(systemZero);

        manager.Initialize();

        // Act
        manager.Update(TimeSpan.FromSeconds(0.016));

        // Assert - All should be called
        Assert.True(systemNegative.UpdateCalled);
        Assert.True(systemZero.UpdateCalled);
        Assert.True(systemPositive.UpdateCalled);
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
