using Engine;
using Engine.Events;
using NLog;

namespace Sandbox;

public class ExampleLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public ExampleLayer(string name) : base(name)
    {
        OnAttach += HandleOnAttach;
        OnDetach += HandleOnDetach;
        OnUpdate += HandleOnUpdate;
    }

    
    public void HandleOnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");
    }

    public void HandleOnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public void HandleOnUpdate()
    {
        Logger.Debug("ExampleLayer OnUpdate.");
    }

    public override void HandleEvent(Event @event)
    {
        base.HandleEvent(@event);
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
    }
}