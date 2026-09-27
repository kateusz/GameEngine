using System.Globalization;
using System.Numerics;
using ECS;
using Editor.UI.Constants;
using Editor.UI.Drawers;
using Editor.UI.FieldEditors;
using ImGuiNET;

namespace Editor.UI.Elements;

public class UIPropertyRenderer(IEnumerable<IFieldEditor> editors)
{
    private readonly Dictionary<Type, IFieldEditor> _editors =
        editors.ToDictionary(editor => editor.ValueType);

    public static void DrawPropertyRow(string label, Action inputControl)
    {
        if (!PropertySearch.Matches(label))
            return;

        var totalWidth = ImGui.GetContentRegionAvail().X;
        ImGui.Columns(2, null, false);
        ImGui.SetColumnWidth(0, totalWidth * EditorUIConstants.PropertyLabelRatio);
        ImGui.SetColumnWidth(1, totalWidth * EditorUIConstants.PropertyInputRatio);
        ImGui.Text(label);
        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1);
        inputControl();
        ImGui.Columns(1);
    }

    public bool TryDrawFieldEditor(string label, Type type, object value, out object newValue)
    {
        newValue = value;

        if (!_editors.TryGetValue(type, out var editor))
        {
            ImGui.TextDisabled($"Unsupported type: {type.Name}");
            return false;
        }

        editor.Draw(label, value, out newValue);
        return true;
    }

    public bool DrawPropertyField(string label, object? value, Action<object> onValueChanged)
    {
        if (value == null)
            return false;

        var valueType = value.GetType();
        if (!_editors.TryGetValue(valueType, out var editor))
        {
            DrawPropertyRow(label, () => ImGui.TextDisabled($"Unsupported type: {valueType.Name}"));
            return false;
        }

        var changed = false;
        DrawPropertyRow(label, () =>
        {
            var inputLabel = $"##{label}";
            editor.Draw(inputLabel, value, out var newValue);
            if (!EqualityComparer<object>.Default.Equals(value, newValue))
            {
                onValueChanged(newValue);
                changed = true;
            }
        });

        return changed;
    }

    public void DrawPropertyField<T>(
        string label,
        Entity entity,
        Func<Entity, T> read,
        Action<Entity, T> write,
        Func<T, T, bool>? same = null)
    {
        UIPropertyRenderer.DrawPropertyRow(label, () =>
        {
            var equals = same ?? EqualityComparer<T>.Default.Equals;
            if (!MultiField.TryUniform(entity, read, equals, out var value))
            {
                DrawBlank(label, entity, read, write);
                return;
            }

            if (!TryDrawFieldEditor($"##{label}", typeof(T), value!, out var boxed))
                return;

            if (!equals(value, (T)boxed))
                MultiField.WriteEach(entity, write, (T)boxed);
        });
    }

    public static void DrawBlankControl(
        string label,
        Type fieldType,
        Entity entity,
        Func<Entity, object> read,
        Action<Entity, object> write) =>
        DrawBlankCore(label, fieldType, entity, read, write);

    private static void DrawBlank<T>(string label, Entity entity, Func<Entity, T> read, Action<Entity, T> write)
    {
        DrawBlankCore(label, typeof(T), entity, e => read(e), (e, v) => write(e, (T)v));
    }

    private static void DrawBlankCore(
        string label,
        Type fieldType,
        Entity entity,
        Func<Entity, object> read,
        Action<Entity, object> write)
    {
        if (fieldType == typeof(float))
        {
            if (MultiField.DrawBlankFloat($"##{label}", out var v))
                MultiField.WriteEach(entity, (Entity e, float f) => write(e, f), v);
            return;
        }

        if (fieldType == typeof(double))
        {
            if (MultiField.DrawBlankFloat($"##{label}", out var v))
                MultiField.WriteEach(entity, (Entity e, double d) => write(e, d), v);
            return;
        }

        if (fieldType == typeof(int))
        {
            var buffer = "";
            if (!ImGui.InputText($"##{label}", ref buffer, 64, ImGuiInputTextFlags.EnterReturnsTrue)
                && !ImGui.IsItemDeactivatedAfterEdit())
                return;

            if (int.TryParse(buffer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                MultiField.WriteEach(entity, (Entity e, int i) => write(e, i), v);
            return;
        }

        if (fieldType == typeof(bool))
        {
            if (ImGui.Button("", new Vector2(ImGui.GetFrameHeight(), ImGui.GetFrameHeight())))
                MultiField.WriteEach(entity, (Entity e, bool b) => write(e, b), true);
            return;
        }

        if (fieldType == typeof(string))
        {
            var buffer = "";
            if (!ImGui.InputText($"##{label}", ref buffer, 256, ImGuiInputTextFlags.EnterReturnsTrue)
                && !ImGui.IsItemDeactivatedAfterEdit())
                return;

            MultiField.WriteEach(entity, (Entity e, string s) => write(e, s), buffer);
            return;
        }

        if (fieldType == typeof(Vector2))
        {
            MultiField.DrawVec2Axes(entity,
                e => (Vector2)read(e),
                (e, v) => write(e, v));
            return;
        }

        if (fieldType == typeof(Vector3))
        {
            MultiField.DrawVec3Axes(entity,
                e => (Vector3)read(e),
                (e, v) => write(e, v));
            return;
        }

        if (fieldType == typeof(Vector4))
            DrawBlankVector4(entity, write);
    }

    private static void DrawBlankVector4(Entity entity, Action<Entity, object> write)
    {
        if (!ImGui.Button("", new Vector2(-1, ImGui.GetFrameHeight())))
            return;

        ImGui.OpenPopup("##MultiColorPopup");
        if (!ImGui.BeginPopup("##MultiColorPopup"))
            return;

        var color = Vector4.One;
        if (LayoutDrawer.DrawColorEdit4("##color", ref color))
            MultiField.WriteEach(entity, (Entity e, Vector4 c) => write(e, c), color);

        ImGui.EndPopup();
    }

    public static bool SameVector4(Vector4 a, Vector4 b) =>
        MultiField.SameFloat(a.X, b.X) && MultiField.SameFloat(a.Y, b.Y)
        && MultiField.SameFloat(a.Z, b.Z) && MultiField.SameFloat(a.W, b.W);

    public static bool SameVector2(Vector2 a, Vector2 b) =>
        MultiField.SameFloat(a.X, b.X) && MultiField.SameFloat(a.Y, b.Y);

    public static bool SameVector3(Vector3 a, Vector3 b) =>
        MultiField.SameFloat(a.X, b.X) && MultiField.SameFloat(a.Y, b.Y)
        && MultiField.SameFloat(a.Z, b.Z);

    public static bool SameDouble(double a, double b) =>
        MultiField.SameFloat((float)a, (float)b);

    public static bool ValuesSame(Type fieldType, object a, object b) =>
        fieldType switch
        {
            not null when fieldType == typeof(float) => MultiField.SameFloat((float)a, (float)b),
            not null when fieldType == typeof(double) => SameDouble((double)a, (double)b),
            not null when fieldType == typeof(Vector2) => SameVector2((Vector2)a, (Vector2)b),
            not null when fieldType == typeof(Vector3) => SameVector3((Vector3)a, (Vector3)b),
            not null when fieldType == typeof(Vector4) => SameVector4((Vector4)a, (Vector4)b),
            _ => EqualityComparer<object>.Default.Equals(a, b)
        };
}
