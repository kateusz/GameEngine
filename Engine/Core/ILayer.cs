using Engine.Core.Input;
using Engine.Events;
using Engine.Events.Input;
using Engine.Events.Window;

namespace Engine.Core;

/// <summary>
/// Defines the interface for application layers that handle events, updates, and rendering.
/// Layers are managed in a stack by the <see cref="Application"/> class.
/// </summary>
/// <remarks>
/// <para><b>Layer Lifecycle:</b></para>
/// <list type="number">
/// <item><description><see cref="OnAttach"/> - Called when the layer is added to the application and resources should be initialized.</description></item>
/// <item><description><see cref="OnUpdate"/> - Called every frame during the application update loop.</description></item>
/// <item><description><see cref="OnDetach"/> - Called when the layer is removed or during application shutdown. All resources (GPU resources, file handles, event subscriptions, etc.) must be released here.</description></item>
/// </list>
/// <para><b>Resource Management:</b></para>
/// <para>Implementations must properly manage resources following RAII principles:</para>
/// <list type="bullet">
/// <item><description>Allocate resources in <see cref="OnAttach"/> (textures, buffers, shaders, subscriptions)</description></item>
/// <item><description>Release resources in <see cref="OnDetach"/> to prevent memory and resource leaks</description></item>
/// <item><description><see cref="OnDetach"/> is guaranteed to be called during application shutdown and when using <c>Application.PopLayer()</c></description></item>
/// </list>
/// </remarks>
public interface ILayer
{
    /// <summary>
    /// Called when the layer is attached to the application.
    /// Use this method to initialize resources, subscribe to events, and perform setup.
    /// </summary>
    /// <param name="inputSystem">The input system for handling user input.</param>
    void OnAttach(IInputSystem inputSystem);

    /// <summary>
    /// Called when the layer is detached from the application or during shutdown.
    /// Implementations MUST release all allocated resources (GPU resources, file handles, event subscriptions, etc.) to prevent leaks.
    /// This method is guaranteed to be called during application shutdown and when using <c>Application.PopLayer()</c>.
    /// </summary>
    void OnDetach();

    /// <summary>
    /// Called every frame during the application update loop.
    /// </summary>
    /// <param name="timeSpan">The time elapsed since the last frame.</param>
    void OnUpdate(TimeSpan timeSpan);

    /// <summary>
    /// Called during the ImGui rendering phase.
    /// Use this method to render ImGui UI elements.
    /// </summary>
    void OnImGuiRender();

    /// <summary>
    /// Handles input events (keyboard, mouse, etc.).
    /// Events are processed in reverse layer order (overlay layers first).
    /// </summary>
    /// <param name="windowEvent">The input event to handle.</param>
    void HandleInputEvent(InputEvent windowEvent);

    /// <summary>
    /// Handles window events (resize, focus, etc.).
    /// Events are processed in layer order.
    /// </summary>
    /// <param name="windowEvent">The window event to handle.</param>
    void HandleWindowEvent(WindowEvent windowEvent);
}
