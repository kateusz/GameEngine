using System.Numerics;
using Editor.UI.Elements;
using ImGuiNET;

namespace Editor.ComponentEditors.Physics;

internal static class ColliderPointListDrawer
{
    public static void DrawVec2List(string label, List<Vector2> points, int minCount)
    {
        ImGui.Text(label);
        for (var i = 0; i < points.Count; i++)
        {
            var point = points[i];
            VectorPanel.DrawVec2Control($"{label}[{i}]", ref point);
            points[i] = point;

            if (points.Count > minCount && ImGui.Button($"Remove##{label}{i}"))
            {
                points.RemoveAt(i);
                break;
            }
        }

        if (ImGui.Button($"Add {label}"))
        {
            var last = points.Count > 0 ? points[^1] : Vector2.Zero;
            points.Add(last + new Vector2(0.5f, 0f));
        }
    }
}
