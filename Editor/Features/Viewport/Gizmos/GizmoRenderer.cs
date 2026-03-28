using System.Numerics;
using Editor.Features.Viewport;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.Features.Viewport.Gizmos;

public static class GizmoRenderer
{
    private const float ArrowLength = 75f;
    private const float ArrowHeadLen = 11f;
    private const float ArrowHeadWidth = 5f;
    private const float LineThickness = 2.5f;
    private const float CubeHandleHalf = 5f;
    private const float FreeHandleHalf = 7f;
    private const float RotationRadius = 60f;
    private const float HoverThreshold = 8f;

    public static GizmoAxis GetTranslationHover(
        Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp, Vector2 localMouse)
    {
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var mouse = ToGlobal(localMouse, viewportBounds);

        var xEnd = AxisEnd(worldPos, Vector3.UnitX, viewportBounds, vp, origin);
        var yEnd = AxisEnd(worldPos, Vector3.UnitY, viewportBounds, vp, origin);
        
        if (IsNearPoint(mouse, origin, FreeHandleHalf + 3f)) return GizmoAxis.Free;
        if (IsNearPoint(mouse, xEnd, CubeHandleHalf + 4f)) return GizmoAxis.X;
        if (IsNearPoint(mouse, yEnd, CubeHandleHalf + 4f)) return GizmoAxis.Y;
        if (IsNearSegment(mouse, origin, xEnd, HoverThreshold)) return GizmoAxis.X;
        if (IsNearSegment(mouse, origin, yEnd, HoverThreshold)) return GizmoAxis.Y;

        return GizmoAxis.None;
    }

    public static void DrawTranslation(
        Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp, GizmoAxis hover)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var xEnd = AxisEnd(worldPos, Vector3.UnitX, viewportBounds, vp, origin);
        var yEnd = AxisEnd(worldPos, Vector3.UnitY, viewportBounds, vp, origin);

        DrawArrow(drawList, origin, xEnd, hover == GizmoAxis.X, EditorUIConstants.AxisXColor);
        DrawArrow(drawList, origin, yEnd, hover == GizmoAxis.Y, EditorUIConstants.AxisYColor);

        var freeCol = GetColor(new Vector4(0.9f, 0.9f, 0f, 0.85f), hover == GizmoAxis.Free);
        drawList.AddRectFilled(
            origin - new Vector2(FreeHandleHalf, FreeHandleHalf),
            origin + new Vector2(FreeHandleHalf, FreeHandleHalf),
            freeCol);
    }
    
    public static GizmoAxis GetScaleHover(
        Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp, Vector2 localMouse)
    {
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var mouse = ToGlobal(localMouse, viewportBounds);

        var xEnd = AxisEnd(worldPos, Vector3.UnitX, viewportBounds, vp, origin);
        var yEnd = AxisEnd(worldPos, Vector3.UnitY, viewportBounds, vp, origin);

        if (IsNearPoint(mouse, origin, FreeHandleHalf + 3f)) return GizmoAxis.Free;
        if (IsNearPoint(mouse, xEnd, CubeHandleHalf + 4f)) return GizmoAxis.X;
        if (IsNearPoint(mouse, yEnd, CubeHandleHalf + 4f)) return GizmoAxis.Y;
        if (IsNearSegment(mouse, origin, xEnd, HoverThreshold)) return GizmoAxis.X;
        if (IsNearSegment(mouse, origin, yEnd, HoverThreshold)) return GizmoAxis.Y;

        return GizmoAxis.None;
    }

    public static void DrawScale(
        Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp, GizmoAxis hover)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var xEnd = AxisEnd(worldPos, Vector3.UnitX, viewportBounds, vp, origin);
        var yEnd = AxisEnd(worldPos, Vector3.UnitY, viewportBounds, vp, origin);

        DrawLine(drawList, origin, xEnd, hover == GizmoAxis.X, EditorUIConstants.AxisXColor);
        DrawLine(drawList, origin, yEnd, hover == GizmoAxis.Y, EditorUIConstants.AxisYColor);
        DrawCube(drawList, xEnd, hover == GizmoAxis.X, EditorUIConstants.AxisXColor);
        DrawCube(drawList, yEnd, hover == GizmoAxis.Y, EditorUIConstants.AxisYColor);

        var freeCol = GetColor(new Vector4(0.9f, 0.9f, 0f, 0.85f), hover == GizmoAxis.Free);
        drawList.AddRectFilled(
            origin - new Vector2(FreeHandleHalf, FreeHandleHalf),
            origin + new Vector2(FreeHandleHalf, FreeHandleHalf),
            freeCol);
    }
    
    public static bool GetRotationHover(
        Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp, Vector2 localMouse)
    {
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var mouse = ToGlobal(localMouse, viewportBounds);
        return MathF.Abs(Vector2.Distance(mouse, origin) - RotationRadius) < HoverThreshold;
    }

    public static void DrawRotation(
        Vector3 worldPos, float rotationZ, Vector2[] viewportBounds, Matrix4x4 vp, bool hover)
    {
        var drawList = ImGui.GetWindowDrawList();
        var origin = WorldToGlobal(worldPos, viewportBounds, vp);
        var col = GetColor(EditorUIConstants.AxisZColor with { W = 0.9f }, hover);

        drawList.AddCircle(origin, RotationRadius, col, 64, 2f);

        // Rotation indicator line
        var indicatorEnd = origin + new Vector2(
            MathF.Cos(-rotationZ) * RotationRadius,
            MathF.Sin(-rotationZ) * RotationRadius);
        drawList.AddLine(origin, indicatorEnd, col, 2f);
    }
    
    private static Vector2 AxisEnd(
        Vector3 worldPos, Vector3 axis,
        Vector2[] viewportBounds, Matrix4x4 vp, Vector2 screenOrigin)
    {
        var axisTip = WorldToGlobal(worldPos + axis, viewportBounds, vp);
        var delta = axisTip - screenOrigin;
        var dir = delta.LengthSquared() > 0.01f
            ? Vector2.Normalize(delta)
            : axis.X != 0 ? Vector2.UnitX : -Vector2.UnitY;
        return screenOrigin + dir * ArrowLength;
    }

    private static void DrawArrow(ImDrawListPtr dl, Vector2 from, Vector2 to, bool hover, Vector4 color)
    {
        var dir = Vector2.Normalize(to - from);
        var col = GetColor(color, hover);
        var shaft = to - dir * ArrowHeadLen;

        dl.AddLine(from, shaft, col, LineThickness);

        var perp = new Vector2(-dir.Y, dir.X);
        dl.AddTriangleFilled(to, shaft + perp * ArrowHeadWidth, shaft - perp * ArrowHeadWidth, col);
    }

    private static void DrawLine(ImDrawListPtr dl, Vector2 from, Vector2 to, bool hover, Vector4 color)
    {
        dl.AddLine(from, to, GetColor(color, hover), LineThickness);
    }

    private static void DrawCube(ImDrawListPtr dl, Vector2 pos, bool hover, Vector4 color)
    {
        var half = CubeHandleHalf + (hover ? 1.5f : 0f);
        dl.AddRectFilled(pos - new Vector2(half, half), pos + new Vector2(half, half), GetColor(color, hover));
    }

    private static uint GetColor(Vector4 color, bool hover)
    {
        if (!hover) return ImGui.ColorConvertFloat4ToU32(color);
        return ImGui.ColorConvertFloat4ToU32(new Vector4(
            MathF.Min(1f, color.X + 0.25f),
            MathF.Min(1f, color.Y + 0.25f),
            MathF.Min(1f, color.Z + 0.25f),
            1f));
    }

    private static Vector2 WorldToGlobal(Vector3 worldPos, Vector2[] viewportBounds, Matrix4x4 vp)
        => ViewportCoordinateConverter.WorldToScreen(worldPos, viewportBounds, vp);

    private static Vector2 ToGlobal(Vector2 localPos, Vector2[] viewportBounds)
        => localPos + viewportBounds[0];

    private static bool IsNearPoint(Vector2 p, Vector2 target, float r)
        => Vector2.Distance(p, target) <= r;

    private static bool IsNearSegment(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        var ab = b - a;
        var len = ab.Length();
        if (len < 0.001f) return false;
        var t = MathF.Max(0f, MathF.Min(len, Vector2.Dot(p - a, ab / len)));
        return Vector2.Distance(p, a + ab / len * t) <= r;
    }
}
