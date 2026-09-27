using System.Numerics;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Drawers;

public readonly struct InputModalOptions(
    string? validationMessage = null,
    string? errorMessage = null,
    bool isValid = true,
    string okLabel = "OK",
    string cancelLabel = "Cancel",
    bool showCancel = false)
{
    public string? ValidationMessage { get; } = validationMessage;
    public string? ErrorMessage { get; } = errorMessage;
    public bool IsValid { get; } = isValid;
    public string OkLabel { get; } = okLabel;
    public string CancelLabel { get; } = cancelLabel;
    public bool ShowCancel { get; } = showCancel;
}

/// <summary>
/// Utility class for common ImGui modal and popup patterns.
/// </summary>
public static class ModalDrawer
{
    public const ImGuiWindowFlags FormModalFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings;

    // PushFont(title) / Begin / PushFont(body) … PopFont / End / PopFont — see imgui#1767.
    private static int _titleFontDepth;

    /// <summary>
    /// Begins a centered modal popup with standard flags.
    /// Must be followed by EndModal() when the modal is closed.
    /// </summary>
    public static bool BeginCenteredModal(
        string title,
        ref bool isOpen,
        ImGuiWindowFlags additionalFlags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMove,
        float minWidth = EditorUIConstants.ModalMinWidth)
    {
        if (isOpen)
            ImGui.OpenPopup(title);

        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (minWidth > 0f)
            ImGui.SetNextWindowSizeConstraints(new Vector2(minWidth, 0f), new Vector2(float.MaxValue, float.MaxValue));

        var fonts = ImGui.GetIO().Fonts.Fonts;
        var hasTitleFont = fonts.Size >= 2;
        if (hasTitleFont)
            ImGui.PushFont(fonts[1]);

        var modalOpen = isOpen;
        var result = ImGui.BeginPopupModal(title, ref modalOpen, additionalFlags | ImGuiWindowFlags.NoSavedSettings);
        isOpen = modalOpen;

        if (hasTitleFont)
        {
            if (result)
            {
                ImGui.PushFont(fonts[0]);
                _titleFontDepth++;
            }
            else
                ImGui.PopFont();
        }

        return result;
    }

    public static void EndModal()
    {
        if (_titleFontDepth > 0)
        {
            ImGui.PopFont();
            _titleFontDepth--;
            ImGui.EndPopup();
            ImGui.PopFont();
        }
        else
            ImGui.EndPopup();
    }

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
        var options = new InputModalOptions(validationMessage, errorMessage, isValid, okLabel, cancelLabel);
        RenderInputModal(title, ref showModal, promptText, ref inputValue, maxLength, onOk, onCancel, options);
    }

    public static void RenderInputModal(
        string title,
        ref bool showModal,
        string promptText,
        ref string inputValue,
        uint maxLength,
        Action onOk,
        Action onCancel,
        InputModalOptions options = default)
    {
        if (!BeginCenteredModal(title, ref showModal, FormModalFlags))
            return;

        LayoutDrawer.DrawFormLabel(TrimPromptLabel(promptText));
        ImGui.SetNextItemWidth(-1f);
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        var enterPressed = ImGui.InputText($"##{title}_Input", ref inputValue, maxLength,
            ImGuiInputTextFlags.EnterReturnsTrue);

        // Multi-line prompts (e.g. "Will create: Foo") go under the field.
        var hint = ExtractPromptHint(promptText);
        if (!string.IsNullOrEmpty(hint))
        {
            ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.InfoColor);
            ImGui.TextWrapped(hint);
            ImGui.PopStyleColor();
        }

        LayoutDrawer.DrawSeparatorWithSpacing();

        if (!string.IsNullOrEmpty(options.ValidationMessage))
            DrawErrorMessage(options.ValidationMessage);

        if (!string.IsNullOrEmpty(options.ErrorMessage))
            DrawErrorMessage(options.ErrorMessage);

        HandleInputModalActions(
            ref showModal,
            enterPressed && options.IsValid,
            options.IsValid,
            options.OkLabel,
            options.CancelLabel,
            onOk,
            onCancel,
            options.ShowCancel);

        EndModal();
    }

    private static string TrimPromptLabel(string promptText)
    {
        var firstLine = promptText.Split('\n')[0].Trim();
        return firstLine.TrimEnd(':').Trim();
    }

    private static string? ExtractPromptHint(string promptText)
    {
        var parts = promptText.Split('\n', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1].Trim() : null;
    }

    private static void HandleInputModalActions(
        ref bool showModal,
        bool shouldExecuteOk,
        bool isValid,
        string okLabel,
        string cancelLabel,
        Action onOk,
        Action onCancel,
        bool showCancel = false)
    {
        var shouldClose = false;
        var actionExecuted = false;

        if (showCancel)
        {
            ButtonDrawer.DrawCenteredModalButtonPair(
                okLabel: okLabel,
                cancelLabel: cancelLabel,
                onOk: () =>
                {
                    if (!actionExecuted) { shouldClose = true; actionExecuted = true; onOk(); }
                },
                onCancel: () =>
                {
                    if (!actionExecuted) { shouldClose = true; actionExecuted = true; onCancel(); }
                },
                okDisabled: !isValid);
        }
        else if (ButtonDrawer.DrawCenteredModalButton(okLabel, disabled: !isValid) && !actionExecuted)
        {
            shouldClose = true;
            actionExecuted = true;
            onOk();
        }

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
    }

    public static void RenderConfirmationModal(
        string title,
        ref bool showModal,
        string message,
        Action onOk,
        Action? onCancel = null,
        string okLabel = "OK",
        string cancelLabel = "Cancel")
    {
        _ = cancelLabel;

        if (!BeginCenteredModal(title, ref showModal, FormModalFlags))
            return;

        ImGui.TextWrapped(message);
        LayoutDrawer.DrawSeparatorWithSpacing();

        var shouldClose = false;

        if (ButtonDrawer.DrawCenteredModalButton(okLabel))
        {
            shouldClose = true;
            onOk();
        }

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

        EndModal();
    }

    public static void RenderMessageBox(
        string title,
        ref bool showModal,
        string message,
        MessageType messageType = MessageType.Info,
        Action? onClose = null)
    {
        if (!BeginCenteredModal(title, ref showModal, FormModalFlags))
            return;

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

        LayoutDrawer.DrawSeparatorWithSpacing();

        if (ButtonDrawer.DrawCenteredModalButton("OK"))
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

        EndModal();
    }

    public static void RenderListSelectionModal(
        string title,
        ref bool showModal,
        string[] items,
        Action<string> onItemSelected,
        Action? onCancel = null,
        string emptyMessage = "No items available.",
        Func<string, int, bool>? renderItem = null)
    {
        if (!BeginCenteredModal(title, ref showModal, FormModalFlags))
            return;

        if (items.Length == 0)
            DrawWarningMessage(emptyMessage);
        else
            RenderItemList(title, items, renderItem, onItemSelected, ref showModal);

        LayoutDrawer.DrawSeparatorWithSpacing();

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            showModal = false;
            onCancel?.Invoke();
        }

        EndModal();
    }

    private static void RenderItemList(
        string title,
        string[] items,
        Func<string, int, bool>? renderItem,
        Action<string> onItemSelected,
        ref bool showModal)
    {
        var itemHeight = ImGui.GetTextLineHeightWithSpacing();
        var visibleItems = System.Math.Min(items.Length, EditorUIConstants.MaxVisibleListItems);
        var listboxHeight = itemHeight * visibleItems + ImGui.GetStyle().FramePadding.Y * 2;

        ImGui.BeginChild($"{title}_List", new Vector2(EditorUIConstants.SelectorListBoxWidth, listboxHeight));

        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            var itemClicked = renderItem != null
                ? renderItem(item, i)
                : ImGui.Selectable(item, false, ImGuiSelectableFlags.DontClosePopups);

            if (itemClicked)
            {
                showModal = false;
                onItemSelected(item);
            }
        }

        ImGui.EndChild();
    }

    private static void DrawErrorMessage(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.ErrorColor);
        ImGui.TextWrapped(errorMessage);
        ImGui.PopStyleColor();
    }

    private static void DrawWarningMessage(string warningMessage)
    {
        if (string.IsNullOrEmpty(warningMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.WarningColor);
        ImGui.TextWrapped(warningMessage);
        ImGui.PopStyleColor();
    }

    private static void DrawSuccessMessage(string successMessage)
    {
        if (string.IsNullOrEmpty(successMessage)) return;

        ImGui.PushStyleColor(ImGuiCol.Text, EditorUIConstants.SuccessColor);
        ImGui.TextWrapped(successMessage);
        ImGui.PopStyleColor();
    }
}
