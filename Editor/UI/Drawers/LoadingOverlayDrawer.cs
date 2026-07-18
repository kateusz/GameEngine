using System.Numerics;
using ImGuiNET;

namespace Editor.UI.Drawers;

public static class LoadingOverlayDrawer
{
    public static void Draw(ImDrawListPtr drawList, in Vector2 position, in Vector2 size, ref float spinnerRotation, string message)
    {
        drawList.AddRectFilled(
            position,
            position + size,
            ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f)));

        var center = position + size * 0.5f;
        spinnerRotation += ImGui.GetIO().DeltaTime * 3.0f;

        const float spinnerRadius = 30.0f;
        const int segments = 12;
        const float thickness = 4.0f;

        for (var i = 0; i < segments; i++)
        {
            var angle = (spinnerRotation + (i * MathF.PI * 2.0f / segments)) % (MathF.PI * 2.0f);
            var alpha = 1.0f - (i / (float)segments);

            drawList.PathArcTo(
                center,
                spinnerRadius,
                angle,
                angle + (MathF.PI * 2.0f / segments * 0.8f),
                10);

            drawList.PathStroke(
                ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, alpha)),
                0,
                thickness);
        }

        var textSize = ImGui.CalcTextSize(message);
        drawList.AddText(
            new Vector2(center.X - textSize.X * 0.5f, center.Y + spinnerRadius + 20),
            ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
            message);
    }
}
