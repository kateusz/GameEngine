using System.Numerics;
using Editor.UI.Constants;
using Editor.Utilities;
using ImGuiNET;

namespace Editor.UI.Drawers;

/// <summary>
/// Utility class for common ImGui modal and popup patterns.
/// </summary>
public static class ModalDrawer
{
    /// <summary>
    /// Begins a centered modal popup with standard flags.
    /// Must be followed by EndModal() when the modal is closed.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="isOpen">Reference to bool controlling modal open state</param>
    /// <param name="additionalFlags">Additional window flags (default: AlwaysAutoResize | NoMove)</param>
    /// <returns>True if the modal is currently open and rendering</returns>
    public static bool BeginCenteredModal(string title, ref bool isOpen,
        ImGuiWindowFlags additionalFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove)
    {
        if (isOpen)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        // Use a local variable to track the actual modal state
        // This allows ImGui to properly manage the popup lifecycle
        var modalOpen = isOpen;
        var result = ImGui.BeginPopupModal(title, ref modalOpen, additionalFlags);

        // Sync the modal state back to the caller's variable
        isOpen = modalOpen;

        return result;
    }

    /// <summary>
    /// Ends a modal popup. Call after BeginCenteredModal returns true.
    /// </summary>
    public static void EndModal() => ImGui.EndPopup();

    /// <summary>
    /// Renders a complete input modal with label, input field, validation, and buttons.
    /// Handles Enter key for confirmation and Escape key for cancellation.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="showModal">Reference to bool controlling modal visibility (set to true to show)</param>
    /// <param name="promptText">Text prompt above the input field</param>
    /// <param name="inputValue">Reference to the input value</param>
    /// <param name="maxLength">Maximum input length</param>
    /// <param name="validationMessage">Optional validation error message</param>
    /// <param name="errorMessage">Optional general error message</param>
    /// <param name="isValid">Whether the current input is valid</param>
    /// <param name="onOk">Callback when OK is clicked or Enter is pressed</param>
    /// <param name="onCancel">Callback when Cancel is clicked or Escape is pressed</param>
    /// <param name="okLabel">Label for OK button (default: "OK")</param>
    /// <param name="cancelLabel">Label for Cancel button (default: "Cancel")</param>
    public static void RenderInputModal(
        string title,
        ref bool showModal,
        string promptText,
        ref string inputValue,
        uint maxLength,
        string? validationMessage,
        string? errorMessage,
        bool isValid,
        Action onOk,
        Action onCancel,
        string okLabel = "OK",
        string cancelLabel = "Cancel")
    {
        if (showModal)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var modalOpen = showModal;
        if (ImGui.BeginPopupModal(title, ref modalOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.Text(promptText);

            // Set focus on input field when modal opens
            if (ImGui.IsWindowAppearing())
                ImGui.SetKeyboardFocusHere();

            var enterPressed = ImGui.InputText($"##{title}_Input", ref inputValue, maxLength,
                ImGuiInputTextFlags.EnterReturnsTrue);

            ImGui.Separator();

            if (!string.IsNullOrEmpty(validationMessage))
                DrawErrorMessage(validationMessage);

            if (!string.IsNullOrEmpty(errorMessage))
                DrawErrorMessage(errorMessage);

            // Handle Enter key for OK action
            var shouldExecuteOk = enterPressed && isValid;
            var shouldClose = false;
            var actionExecuted = false;

            ButtonDrawer.DrawModalButtonPair(
                okLabel: okLabel,
                cancelLabel: cancelLabel,
                onOk: () =>
                {
                    if (!actionExecuted)
                    {
                        shouldClose = true;
                        actionExecuted = true;
                        onOk();
                    }
                },
                onCancel: () =>
                {
                    if (!actionExecuted)
                    {
                        shouldClose = true;
                        actionExecuted = true;
                        onCancel();
                    }
                },
                okDisabled: !isValid);

            if (shouldExecuteOk && !actionExecuted)
            {
                shouldClose = true;
                actionExecuted = true;
                onOk();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape) && !actionExecuted)
            {
                shouldClose = true;
                actionExecuted = true;
                onCancel();
            }

            if (shouldClose)
                showModal = false;

            ImGui.EndPopup();
        }
        else
        {
            showModal = modalOpen;
        }
    }

    /// <summary>
    /// Renders a simple confirmation modal with OK/Cancel buttons.
    /// Handles Enter key for confirmation and Escape key for cancellation.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="showModal">Reference to bool controlling modal visibility</param>
    /// <param name="message">Message to display</param>
    /// <param name="onOk">Callback when OK is clicked or Enter is pressed</param>
    /// <param name="onCancel">Optional callback when Cancel is clicked or Escape is pressed</param>
    /// <param name="okLabel">Label for OK button (default: "OK")</param>
    /// <param name="cancelLabel">Label for Cancel button (default: "Cancel")</param>
    public static void RenderConfirmationModal(
        string title,
        ref bool showModal,
        string message,
        Action onOk,
        Action? onCancel = null,
        string okLabel = "OK",
        string cancelLabel = "Cancel")
    {
        if (showModal)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        var modalOpen = showModal;
        if (ImGui.BeginPopupModal(title, ref modalOpen,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            ImGui.TextWrapped(message);
            ImGui.Separator();

            var shouldClose = false;

            ButtonDrawer.DrawModalButtonPair(
                okLabel: okLabel,
                cancelLabel: cancelLabel,
                onOk: () =>
                {
                    shouldClose = true;
                    onOk();
                },
                onCancel: () =>
                {
                    shouldClose = true;
                    onCancel?.Invoke();
                });

            if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
            {
                shouldClose = true;
                onOk();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                shouldClose = true;
                onCancel?.Invoke();
            }

            if (shouldClose)
                showModal = false;

            ImGui.EndPopup();
        }
        else
        {
            showModal = modalOpen;
        }
    }

    /// <summary>
    /// Renders a message box modal with a single OK button.
    /// Handles Enter and Escape keys to close the modal.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="showModal">Reference to bool controlling modal visibility</param>
    /// <param name="message">Message to display</param>
    /// <param name="messageType">Type of message (affects color)</param>
    /// <param name="onClose">Optional callback when the modal is closed</param>
    public static void RenderMessageBox(
        string title,
        ref bool showModal,
        string message,
        MessageType messageType = MessageType.Info,
        Action? onClose = null)
    {
        if (showModal)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal(title, ref showModal,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            switch (messageType)
            {
                case MessageType.Error:
                    DrawErrorMessage(message);
                    break;
                case MessageType.Warning:
                    DrawWarningMessage(message);
                    break;
                case MessageType.Success:
                    DrawSuccessMessage(message);
                    break;
                default:
                    ImGui.TextWrapped(message);
                    break;
            }

            ImGui.Separator();

            if (ButtonDrawer.DrawModalButton("OK"))
            {
                showModal = false;
                onClose?.Invoke();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Enter) ||
                ImGui.IsKeyPressed(ImGuiKey.KeypadEnter) ||
                ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                showModal = false;
                onClose?.Invoke();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Renders a list selection modal with search/filter capability.
    /// </summary>
    /// <param name="title">Modal title</param>
    /// <param name="showModal">Reference to bool controlling modal visibility</param>
    /// <param name="items">Array of items to display</param>
    /// <param name="onItemSelected">Callback when an item is selected</param>
    /// <param name="onCancel">Optional callback when Cancel is clicked</param>
    /// <param name="emptyMessage">Message to display when no items are available</param>
    /// <param name="renderItem">Optional custom rendering function for each item (returns true if item was clicked)</param>
    public static void RenderListSelectionModal(
        string title,
        ref bool showModal,
        string[] items,
        Action<string> onItemSelected,
        Action? onCancel = null,
        string emptyMessage = "No items available.",
        Func<string, int, bool>? renderItem = null)
    {
        if (showModal)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal(title, ref showModal,
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove))
        {
            if (items.Length == 0)
            {
                DrawWarningMessage(emptyMessage);
            }
            else
            {
                // Calculate proper height for the listbox
                var itemHeight = ImGui.GetTextLineHeightWithSpacing();
                var visibleItems = Math.Min(items.Length, EditorUIConstants.MaxVisibleListItems);
                var listboxHeight = itemHeight * visibleItems + ImGui.GetStyle().FramePadding.Y * 2;

                ImGui.BeginChild($"{title}_List", new Vector2(EditorUIConstants.SelectorListBoxWidth, listboxHeight));

                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i];
                    var itemClicked = false;

                    if (renderItem != null)
                    {
                        itemClicked = renderItem(item, i);
                    }
                    else
                    {
                        if (ImGui.Selectable(item, false, ImGuiSelectableFlags.DontClosePopups))
                        {
                            itemClicked = true;
                        }
                    }

                    if (itemClicked)
                    {
                        showModal = false;
                        onItemSelected(item);
                    }
                }

                ImGui.EndChild();
            }

            ImGui.Separator();

            if (ButtonDrawer.DrawModalButton("Cancel"))
            {
                showModal = false;
                onCancel?.Invoke();
            }

            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                showModal = false;
                onCancel?.Invoke();
            }

            ImGui.EndPopup();
        }
    }

    /// <summary>
    /// Draws an error message with standard red color and wrapping.
    /// </summary>
    /// <param name="errorMessage">Error message to display</param>
    private static void DrawErrorMessage(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
        ImGui.TextWrapped(errorMessage);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Draws a warning message with standard yellow color and wrapping.
    /// </summary>
    /// <param name="warningMessage">Warning message to display</param>
    private static void DrawWarningMessage(string warningMessage)
    {
        if (string.IsNullOrEmpty(warningMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.WarningColor);
        ImGui.TextWrapped(warningMessage);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Draws a success message with standard green color and wrapping.
    /// </summary>
    /// <param name="successMessage">Success message to display</param>
    private static void DrawSuccessMessage(string successMessage)
    {
        if (string.IsNullOrEmpty(successMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.SuccessColor);
        ImGui.TextWrapped(successMessage);
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Draws an info message with standard info color and wrapping.
    /// </summary>
    /// <param name="infoMessage">Info message to display</param>
    private static void DrawInfoMessage(string infoMessage)
    {
        if (string.IsNullOrEmpty(infoMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);
        ImGui.TextWrapped(infoMessage);
        ImGui.PopStyleColor();
    }
}