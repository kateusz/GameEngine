# Occlusion Culling — Implementation

C# sketch for the color-pass grid in `developer-guide.md`. Do not add a system, a component, a serializer, or a DryIoc registration. The color call already happens inside `SceneRenderSystem` (priority 150) and the editor viewport, both through `SceneRenderPipeline.RenderScene`.

Depth is `clip.z / clip.w` with this engine's view-projection: **0 at the near plane, 1 at the far plane**. Do not remap z with `* 0.5 + 0.5`. Column and row positions do use that map from normalized-device x and y in `[-1, 1]`. Row 0 is the bottom.

`SceneRenderPipeline` is already large. The math lives in a new file, `Engine/Renderer/SoftwareOcclusionBuffer.cs`. The pipeline keeps one instance in a static field and calls it.

## 1. Grid

`ponytail:` the 90-row grid and the 2,000-triangle budget under-cull once a frame has more useful occluder triangles than that. Raise the budget, or lower the 0.02 screen fraction, before adding a simplified occluder mesh.

```csharp
using System.Numerics;

namespace Engine.Renderer;

internal sealed class SoftwareOcclusionBuffer
{
    public const int Rows = 90;
    public const int MaxColumns = 256;
    public const float MinScreenFraction = 0.02f;
    public const int TriangleBudget = 2000;
    public const float DepthSlack = 1e-5f;
    private const float MinClipW = 1e-8f;

    private float[] _cells = [];
    private Matrix4x4 _viewProjection;
    private int _columns;
    private int _rows;
    private int _submittedTriangles;
    private bool _active;

    public bool IsActive => _active;
    public int SubmittedTriangles => _submittedTriangles;

    public bool TryBegin(Matrix4x4 viewProjection)
    {
        _active = false;
        _submittedTriangles = 0;
        if (!TryAspect(viewProjection, out var aspect))
            return false;

        _columns = Math.Clamp((int)MathF.Round(Rows * aspect), 1, MaxColumns);
        _rows = Rows;
        var count = _columns * _rows;
        if (_cells.Length < count)
            _cells = new float[count];
        Array.Fill(_cells, float.PositiveInfinity, 0, count);
        _viewProjection = viewProjection;
        _active = true;
        return true;
    }

    public void End() => _active = false;

    public bool ScreenFractionAtLeastMinimum(Matrix4x4 world, Aabb local)
    {
        if (!TryProjectBox(world, local, out var ndc, out _))
            return true;

        var min = new Vector2(float.PositiveInfinity);
        var max = new Vector2(float.NegativeInfinity);
        foreach (var point in ndc)
        {
            min = Vector2.Min(min, new Vector2(point.X, point.Y));
            max = Vector2.Max(max, new Vector2(point.X, point.Y));
        }

        var width = MathF.Max(0f, max.X - min.X);
        var height = MathF.Max(0f, max.Y - min.Y);
        return width * height / 4f >= MinScreenFraction;
    }

    public bool IsOccluded(Matrix4x4 world, Aabb local)
    {
        if (!_active || !TryProjectBox(world, local, out var ndc, out var nearest))
            return false;

        PixelRect(ndc, out var x0, out var y0, out var x1, out var y1);
        if (x1 <= x0 || y1 <= y0)
            return false;

        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            var stored = _cells[y * _columns + x];
            if (!float.IsFinite(stored) || nearest <= stored + DepthSlack)
                return false;
        }

        return true;
    }

    public void RasterizeTriangle(Vector3 world0, Vector3 world1, Vector3 world2)
    {
        if (!_active || _submittedTriangles >= TriangleBudget)
            return;
        _submittedTriangles++;
        if (!TryProject(_viewProjection, world0, out var a)
            || !TryProject(_viewProjection, world1, out var b)
            || !TryProject(_viewProjection, world2, out var c)
            || a.Z < 0f || b.Z < 0f || c.Z < 0f)
            return;

        var far = MathF.Max(a.Z, MathF.Max(b.Z, c.Z));
        Span<Vector3> triangle = [a, b, c];
        PixelRect(triangle, out var x0, out var y0, out var x1, out var y1);
        for (var y = y0; y < y1; y++)
        for (var x = x0; x < x1; x++)
        {
            if (!CellFullyInside(x, y, a, b, c))
                continue;
            var index = y * _columns + x;
            if (far < _cells[index])
                _cells[index] = far;
        }
    }

    private static bool TryAspect(Matrix4x4 clip, out float aspect)
    {
        aspect = 0f;
        if (!float.IsFinite(clip.M11) || !float.IsFinite(clip.M22) || MathF.Abs(clip.M11) <= MinClipW)
            return false;
        aspect = clip.M22 / clip.M11;
        return float.IsFinite(aspect) && aspect > 0f;
    }

    private static bool TryProject(Matrix4x4 viewProjection, Vector3 world, out Vector3 ndc)
    {
        var clip = Vector4.Transform(new Vector4(world, 1f), viewProjection);
        ndc = default;
        if (!float.IsFinite(clip.W) || clip.W <= MinClipW)
            return false;
        var inverseW = 1f / clip.W;
        ndc = new Vector3(clip.X * inverseW, clip.Y * inverseW, clip.Z * inverseW);
        return float.IsFinite(ndc.X) && float.IsFinite(ndc.Y) && float.IsFinite(ndc.Z);
    }
}
```

`TryProjectBox` projects the eight corners from `Aabb.TransformCorners`. If any corner fails `TryProject` or has a negative depth, it returns false. Otherwise it returns the eight normalized-device points and the minimum depth.

`PixelRect` converts those x and y values with `(ndc * 0.5 + 0.5) * size`, floors the minimum, ceils the maximum, and clamps to the grid. `CellFullyInside` builds the cell's four corners in normalized-device space — x from `x / columns * 2 - 1` through `(x + 1) / columns * 2 - 1`, and the same for y — and accepts the cell only when every corner has non-negative barycentric coordinates against the triangle. A zero-area triangle fails that test and writes nothing.

`ScreenFractionAtLeastMinimum` is the normalized-device rectangle area divided by 4, and it returns true when the box does not fully project. Callers still refuse cutout meshes and meshes with no CPU triangles before treating that as an occluder. `RasterizeTriangle` counts a triangle as it is submitted, including one it then refuses to write, so the budget stops further work. The write loop visits only the triangle's own cell rectangle.

## 2. Color pass only

Add `bool useSoftwareOcclusion = false` to `DrawOpaque3D`. The color call is the only one that passes `true`:

```csharp
perf.ColorPass = DrawOpaque3D(
    context, graphics3D, textureFactory, modelFactory, view.ViewProjection,
    useSoftwareOcclusion: true,
    vertexArrayFactory: vertexArrayFactory,
    vertexBufferFactory: vertexBufferFactory,
    indexBufferFactory: indexBufferFactory);
```

Shadow calls stay on the overloads they use now.

With the flag off, keep today's immediate `DrawCube` / `QueueSubmesh` path. With the flag on, push those loose survivors onto a reused `List<LooseDraw>` instead of submitting them. Keep the frustum reject, and its `Culled` increment, where it is. Leave merge appends on the path they already take. `LooseDraw` holds the entity id, the draw world matrix, the local box, the tint and material fields the queue already needs, and either the cube flag or the submesh.

After the entity loop:

```csharp
var grid = OcclusionGrid;
if (useSoftwareOcclusion && grid.TryBegin(cullMatrix))
{
    var occluders = GatherOccluders(loose, MergeGroups, grid);
    occluders.Sort((a, b) => a.NearestDepth.CompareTo(b.NearestDepth));
    foreach (var occluder in occluders)
    {
        if (grid.SubmittedTriangles >= SoftwareOcclusionBuffer.TriangleBudget)
            break;
        if (grid.IsOccluded(occluder.World, occluder.Local))
            continue;

        Rasterize(grid, occluder);
    }

    foreach (var draw in loose)
    {
        if (grid.IsOccluded(draw.World, draw.Local))
        {
            stats.OcclusionCulled++;
            continue;
        }

        SubmitLoose(graphics3D, textureFactory, draw, ref stats);
    }
}
else if (useSoftwareOcclusion)
{
    foreach (var draw in loose)
        SubmitLoose(graphics3D, textureFactory, draw, ref stats);
}
```

`GatherOccluders` copies loose draws and merged groups that pass `ScreenFractionAtLeastMinimum`, are not `AlphaCutout`, and have triangles. A group candidate uses the world-space group box already computed for frustum testing (`Matrix4x4.Identity` as its world). Its triangles are the solid members' `BakeVertices` / `BakeIndices`, each multiplied by that member's `World`, not by the identity group matrix. A loose cube uses `MeshFactory.CubeTemplate` and `Aabb.UnitCube`. Nearest depth is the projected minimum, or `0` when the box does not fully project, so that candidate is sorted first. `Rasterize` stops when the budget is spent. A candidate that `continue` skips is not written; submit and flush call `IsOccluded` again, which is still true.

`End()` runs in the color pass's existing `finally`, after `FlushMergeGroups`, so flush can still read the grid.

## 3. Merge flush

`FlushMergeGroups` already records a cache match in `MergeTouchedKeys` and then skips a group whose box is outside the frustum. Put the occlusion continue in that same place, after the cache-match touch, so a hidden group is not evicted and not rebuilt:

```csharp
if (OcclusionGrid.IsActive
    && hasGroupBox
    && OcclusionGrid.IsOccluded(Matrix4x4.Identity, groupBox))
{
    stats.OcclusionCulled++;
    continue;
}
```

That `continue` is what avoids `TryBuildCombined`. A group that was built on an earlier frame stays in `CombinedMeshes`. Camera motion that only changes this answer does not increment `MergedRebuilds`.

Where flush draws a member by itself (`ShouldSkipMergeMember`), skip that member when `IsOccluded` is true for the member's world matrix and local box, and count it. Do not remove the member from `MergeGroups`.

## 4. Stats

`PassStats` gains `OcclusionCulled`. `Accumulate` adds it. `ApplyPipelineStats` copies the color pass into `Statistics.OcclusionCulled` and does not add the shadow passes.

```csharp
public int OcclusionCulled { get; set; }
```

In `Editor/Panels/RendererStatsPanel.cs`, next to the frustum line, in both the panel text and the copied text:

```csharp
ImGui.Text($"Occlusion Culled: {stats.OcclusionCulled}");
```

In `Benchmark/PipelineStatsAggregator.cs`, copy the field in `Copy` and add:

```csharp
result.CustomMetrics["Avg Occlusion Culled"] = Avg(s => s.OcclusionCulled).ToString("F1");
```

No other editor file changes. There is no debug view of the grid.

## 5. Tests

`SoftwareOcclusionBuffer` is constructed directly. No GL and no scene.

- A triangle that covers every cell hides a box whose nearest depth is behind the written depth.
- A box with a smaller depth than that triangle is not hidden.
- A box whose rectangle sticks out of the triangle is not hidden.
- A triangle that misses one corner of a cell does not write the cell, so a box that overlaps only that cell is not hidden.
- A triangle with one vertex at negative depth writes nothing.
- A non-finite vertex writes nothing and does not hide the box.
- `TryBegin` returns false for a matrix whose `M11` is 0, and `IsOccluded` then returns false.

`tests/Engine.Tests/Scene/OcclusionCullingPipelineTests.cs` follows `VisibilityZonePipelineTests`: a recorded `IGraphics3D`, a substitute model factory, and `RenderScene`.

- A large triangle mesh in front of a small mesh drops the small mesh's color draw and increments `OcclusionCulled`.
- The same pair with directional shadows enabled still draws the small mesh in the shadow pass.
- The occluder mesh with `AlphaCutout` set does not hide the mesh behind it.
- A merged group whose box is entirely behind the occluder skips the group draw. After the group has been built once, moving the camera so the group stays behind the occluder leaves `MergedRebuilds` unchanged. A group whose box still extends in front of the occluder keeps its draw.

The buffer tests are the check that fails if coverage or depth is reversed. The scene tests are the check that the color call uses the grid and the shadow calls do not.
