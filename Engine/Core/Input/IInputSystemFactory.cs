using Silk.NET.Input;

namespace Engine.Core.Input;

/// <summary>
/// Factory for creating platform-specific input system implementations.
/// Input systems require IInputContext which is only available after window initialization.
/// </summary>
internal interface IInputSystemFactory
{
    /// <summary>
    /// Creates an input system instance using the provided input context.
    /// Called during window load after IInputContext becomes available.
    /// </summary>
    IInputSystem Create(IInputContext inputContext);
}
