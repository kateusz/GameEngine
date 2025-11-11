namespace Editor.Core;

/// <summary>
/// Event system for inter-panel communication.
/// Avoids tight coupling between UI components by allowing panels to communicate via events.
///
/// Usage:
/// 1. Define event types as records (e.g., public record EntitySelectedEvent(Entity Entity))
/// 2. Subscribe to events using Subscribe<TEvent>(handler)
/// 3. Publish events using Publish<TEvent>(eventData)
/// 4. Remember to unsubscribe in Dispose() to avoid memory leaks
/// </summary>
public class EditorEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    /// <summary>
    /// Subscribe to an event type
    /// </summary>
    /// <typeparam name="TEvent">Event type to subscribe to</typeparam>
    /// <param name="handler">Handler function to call when event is published</param>
    public void Subscribe<TEvent>(Action<TEvent> handler)
    {
        var eventType = typeof(TEvent);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();

        _handlers[eventType].Add(handler);
    }

    /// <summary>
    /// Unsubscribe from an event type
    /// </summary>
    /// <typeparam name="TEvent">Event type to unsubscribe from</typeparam>
    /// <param name="handler">Handler function to remove</param>
    public void Unsubscribe<TEvent>(Action<TEvent> handler)
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            handlers.Remove(handler);

            // Clean up empty handler lists
            if (handlers.Count == 0)
                _handlers.Remove(eventType);
        }
    }

    /// <summary>
    /// Publish an event to all subscribed handlers
    /// </summary>
    /// <typeparam name="TEvent">Event type to publish</typeparam>
    /// <param name="eventData">Event data to pass to handlers</param>
    public void Publish<TEvent>(TEvent eventData)
    {
        var eventType = typeof(TEvent);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            // Create a copy to avoid issues if handlers modify the list
            var handlersCopy = handlers.ToList();

            foreach (var handler in handlersCopy.Cast<Action<TEvent>>())
            {
                handler.Invoke(eventData);
            }
        }
    }

    /// <summary>
    /// Clear all event subscriptions
    /// </summary>
    public void Clear()
    {
        _handlers.Clear();
    }

    /// <summary>
    /// Get the number of subscribers for a specific event type
    /// </summary>
    public int GetSubscriberCount<TEvent>()
    {
        var eventType = typeof(TEvent);
        return _handlers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
    }
}
