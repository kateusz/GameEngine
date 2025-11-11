using System.Collections.Concurrent;
using Serilog;

namespace Engine.Events;

/// <summary>
/// Global event bus for publish-subscribe pattern.
/// Allows decoupled communication between systems, scripts, and components.
/// Thread-safe implementation using concurrent collections.
/// </summary>
public class EventBus
{
    private static readonly ILogger Logger = Log.ForContext<EventBus>();
    
    // Thread-safe dictionary of event type -> list of subscribers
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new();
    
    // Lock for modifying subscriber lists
    private readonly Lock _lock = new();

    /// <summary>
    /// Subscribe to events of type T.
    /// The handler will be called whenever an event of this type is published.
    /// </summary>
    /// <typeparam name="T">Type of event to subscribe to</typeparam>
    /// <param name="handler">Callback to invoke when event is published</param>
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers[eventType] = handlers;
            }

            handlers.Add(handler);
        }

        Logger.Debug("Subscribed to {EventTypeName}, total subscribers: {Count}", eventType.Name, _subscribers[eventType].Count);
    }

    /// <summary>
    /// Unsubscribe from events of type T.
    /// </summary>
    /// <typeparam name="T">Type of event to unsubscribe from</typeparam>
    /// <param name="handler">Handler to remove</param>
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers)) 
                return;
            
            handlers.Remove(handler);
                
            // Clean up empty lists
            if (handlers.Count == 0)
            {
                _subscribers.TryRemove(eventType, out _);
            }
                
            Logger.Debug("Unsubscribed from {EventTypeName}", eventType.Name);
        }
    }

    /// <summary>
    /// Publish an event to all subscribers.
    /// Calls all registered handlers for this event type.
    /// Errors in handlers are logged but don't prevent other handlers from running.
    /// </summary>
    /// <typeparam name="T">Type of event to publish</typeparam>
    /// <param name="event">Event instance to publish</param>
    public void Publish<T>(T @event) where T : class
    {
        var eventType = typeof(T);
        
        if (!_subscribers.TryGetValue(eventType, out var handlers))
        {
            // No subscribers for this event type
            return;
        }

        // Create snapshot of handlers to avoid modification during iteration
        List<Delegate> handlersCopy;
        lock (_lock)
        {
            handlersCopy = new List<Delegate>(handlers);
        }

        // Invoke all handlers
        foreach (var handler in handlersCopy)
        {
            try
            {
                (handler as Action<T>)?.Invoke(@event);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error invoking event handler for {EventTypeName}", eventType.Name);
            }
        }
    }
}

