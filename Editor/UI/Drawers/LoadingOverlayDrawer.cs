using System.Numerics;
using ImGuiNET;

namespace Editor.UI.Drawers;

public static class LoadingOverlayDrawer
{
    public static void DrawFullscreen(string text, ref float spinnerRotation, float? progress = null)
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.SetNextWindowFocus();
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoDocking
            | ImGuiWindowFlags.NoNav;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        ImGui.Begin("##LoadingOverlay", flags);
        ImGui.PopStyleVar(3);
        Draw(text, ref spinnerRotation, progress);
        ImGui.End();
    }

    private static void Draw(string text, ref float spinnerRotation, float? progress)
    {
        var min = ImGui.GetWindowPos();
        var size = ImGui.GetWindowSize();
        var max = min + size;
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(min, max, ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f)));

        var center = (min + max) * 0.5f;
        const float spinnerRadius = 30.0f;
        DrawSpinner(drawList, ref spinnerRotation, center, spinnerRadius, 4.0f);

        var textSize = ImGui.CalcTextSize(text);
        var textY = center.Y + spinnerRadius + 20;
        drawList.AddText(
            new Vector2(center.X - textSize.X * 0.5f, textY),
            ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)),
            text);

        if (progress is not { } fraction)
            return;

        var barWidth = MathF.Min(400f, size.X * 0.4f);
        var barHeight = ImGui.GetFrameHeight();
        var barPos = new Vector2(center.X - barWidth * 0.5f, textY + textSize.Y + 16f);
        ImGui.SetCursorScreenPos(barPos);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
        var clamped = fraction < 0f ? 0f : fraction > 1f ? 1f : fraction;
        ImGui.ProgressBar(clamped, new Vector2(barWidth, barHeight));
        ImGui.PopStyleColor();
    }

    public static void DrawSpinner(
        ImDrawListPtr drawList,
        ref float spinnerRotation,
        Vector2 center,
        float radius,
        float thickness)
    {
        spinnerRotation += ImGui.GetIO().DeltaTime * 3.0f;
        const int segments = 12;
        for (var i = 0; i < segments; i++)
        {
            var angle = (spinnerRotation + (i * MathF.PI * 2.0f / segments)) % (MathF.PI * 2.0f);
            var alpha = 1.0f - (i / (float)segments);
            drawList.PathArcTo(center, radius, angle, angle + (MathF.PI * 2.0f / segments * 0.8f), 10);
            drawList.PathStroke(ImGui.GetColorU32(new Vector4(0.2f, 0.6f, 1.0f, alpha)), 0, thickness);
        }
    }
}
