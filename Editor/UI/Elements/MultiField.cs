using System.Globalization;
using System.Numerics;
using ECS;
using Editor.UI.Constants;
using ImGuiNET;

namespace Editor.UI.Elements;

public static class MultiField
{
    public static Entity[]? Targets { get; set; }

    public static bool SameFloat(float a, float b) =>
        float.IsFinite(a) && float.IsFinite(b) && MathF.Round(a, 2) == MathF.Round(b, 2);

    public static IReadOnlyList<Entity> For(Entity fallback) => Targets ?? [fallback];

    public static bool TryUniform<T>(
        Entity fallback,
        Func<Entity, T> read,
        Func<T, T, bool> same,
        out T value)
    {
        var targets = For(fallback);
        value = default!;
        if (targets.Count == 0)
            return false;

        value = read(targets[0]);
        for (var i = 1; i < targets.Count; i++)
        {
            if (!same(value, read(targets[i])))
                return false;
        }

        return true;
    }

    public static void WriteEach<T>(Entity fallback, Action<Entity, T> write, T value)
    {
        foreach (var entity in For(fallback))
            write(entity, value);
    }

    public static bool DrawBlankFloat(string label, out float value)
    {
        value = 0f;
        var buffer = "";
        if (!ImGui.InputText(label, ref buffer, 64, ImGuiInputTextFlags.EnterReturnsTrue)
            && !ImGui.IsItemDeactivatedAfterEdit())
            return false;

        return float.TryParse(buffer, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
               && float.IsFinite(value);
    }

    public static void DrawVec3Control(
        string label,
        Entity entity,
        Func<Entity, Vector3> read,
        Action<Entity, Vector3> write,
        float resetValue = 0f)
    {
        if (!PropertySearch.Matches(label))
            return;

        ImGui.PushID(label);
        VectorPanel.BeginVectorRow(label, 3, out var inputWidth);
        DrawVec3Axes(entity, read, write, resetValue, inputWidth);
        ImGui.PopID();
        ImGui.Columns(1);
    }

    public static void DrawVec2Control(
        string label,
        Entity entity,
        Func<Entity, Vector2> read,
        Action<Entity, Vector2> write,
        float resetValue = 0f)
    {
        if (!PropertySearch.Matches(label))
            return;

        ImGui.PushID(label);
        VectorPanel.BeginVectorRow(label, 2, out var inputWidth);
        DrawVec2Axes(entity, read, write, resetValue, inputWidth);
        ImGui.PopID();
        ImGui.Columns(1);
    }

    public static void DrawVec3Axes(
        Entity entity,
        Func<Entity, Vector3> read,
        Action<Entity, Vector3> write,
        float resetValue = 0f,
        float? inputWidth = null)
    {
        var width = inputWidth ?? VectorPanel.ComputeAxisInputWidth(3);
        ImGui.PushID("vec3");
        DrawAxis("X", entity, e => read(e).X, SetX(read, write), resetValue, EditorUIConstants.AxisXColor, width);
        ImGui.SameLine();
        DrawAxis("Y", entity, e => read(e).Y, SetY(read, write), resetValue, EditorUIConstants.AxisYColor, width);
        ImGui.SameLine();
        DrawAxis("Z", entity, e => read(e).Z, SetZ(read, write), resetValue, EditorUIConstants.AxisZColor, width);
        ImGui.PopID();
    }

    public static void DrawVec2Axes(
        Entity entity,
        Func<Entity, Vector2> read,
        Action<Entity, Vector2> write,
        float resetValue = 0f,
        float? inputWidth = null)
    {
        var width = inputWidth ?? VectorPanel.ComputeAxisInputWidth(2);
        ImGui.PushID("vec2");
        DrawAxis("X", entity, e => read(e).X, (e, x) =>
        {
            var v = read(e);
            v.X = x;
            write(e, v);
        }, resetValue, EditorUIConstants.AxisXColor, width, drag: false);
        ImGui.SameLine();
        DrawAxis("Y", entity, e => read(e).Y, (e, y) =>
        {
            var v = read(e);
            v.Y = y;
            write(e, v);
        }, resetValue, EditorUIConstants.AxisYColor, width, drag: false);
        ImGui.PopID();
    }

    public static void DrawAxis(
        string axisLabel,
        Entity entity,
        Func<Entity, float> read,
        Action<Entity, float> write,
        float resetValue = 0f,
        Vector4? axisColor = null,
        float? inputWidth = null,
        bool drag = true)
    {
        var color = axisColor ?? EditorUIConstants.AxisXColor;
        var width = inputWidth ?? ImGui.GetContentRegionAvail().X;

        if (!TryUniform(entity, read, SameFloat, out var value))
        {
            DrawMixedAxis(axisLabel, entity, write, resetValue, color, width);
            return;
        }

        var before = value;
        VectorPanel.DrawAxisControl(axisLabel, ref value, resetValue, color, width, drag);
        if (!SameFloat(before, value))
            WriteEach(entity, write, value);
    }

    private static void DrawMixedAxis(
        string axisLabel,
        Entity entity,
        Action<Entity, float> write,
        float resetValue,
        Vector4 color,
        float inputWidth)
    {
        ImGui.PushStyleColor(ImGuiCol.Button, color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color * new Vector4(1.1f, 1.1f, 1.1f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, color);

        if (ImGui.Button(axisLabel, new Vector2(EditorUIConstants.SmallButtonSize, ImGui.GetFrameHeight())))
            WriteEach(entity, write, resetValue);

        ImGui.PopStyleColor(3);

        ImGui.SameLine();
        ImGui.SetNextItemWidth(inputWidth);
        if (DrawBlankFloat($"##{axisLabel}", out var committed))
            WriteEach(entity, write, committed);
    }

    private static Action<Entity, float> SetX(Func<Entity, Vector3> read, Action<Entity, Vector3> write) =>
        (e, x) =>
        {
            var v = read(e);
            v.X = x;
            write(e, v);
        };

    private static Action<Entity, float> SetY(Func<Entity, Vector3> read, Action<Entity, Vector3> write) =>
        (e, y) =>
        {
            var v = read(e);
            v.Y = y;
            write(e, v);
        };

    private static Action<Entity, float> SetZ(Func<Entity, Vector3> read, Action<Entity, Vector3> write) =>
        (e, z) =>
        {
            var v = read(e);
            v.Z = z;
            write(e, v);
        };
}
