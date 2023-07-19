using Engine;
using Engine.Events;
using NLog;

namespace Sandbox;

public class ExampleLayer : Layer
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    public ExampleLayer(string name) : base(name)
    {
    }

    public override void OnAttach()
    {
        Logger.Debug("ExampleLayer OnAttach.");
    }

    public override void OnDetach()
    {
        Logger.Debug("ExampleLayer OnDetach.");
    }

    public override void OnUpdate()
    {
        Logger.Debug("ExampleLayer OnUpdate.");
    }

    public override void OnEvent(Event @event)
    {
        Logger.Debug("ExampleLayer OnEvent: {0}", @event);
    }
}