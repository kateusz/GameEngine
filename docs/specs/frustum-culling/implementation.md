# Frustum Culling — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. Tests are new files plus additions to the shadow pipeline tests and the mesh upload test.

Clip Z is **0 at the near plane** and **1 at the far plane**, for `CreatePerspectiveFieldOfView` and for the hand-written light ortho. Do not extract an OpenGL near plane at clip Z −1. The outside slack is **1e-4** world units. A normal shorter than **1e-8** means plane extraction failed.

## 1. Local box

Add `Engine/Renderer/Aabb.cs`. The cube half-extent lives here so `MeshFactory` and the walk share it.

```csharp
using System.Numerics;
using Engine.Renderer.Meshes;

namespace Engine.Renderer;

internal readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public const float UnitCubeHalfExtent = 0.5f;

    public static Aabb UnitCube { get; } = new(
        new Vector3(-UnitCubeHalfExtent),
        new Vector3(UnitCubeHalfExtent));

    public static Aabb? FromPositions(List<Mesh.Vertex> vertices)
    {
        if (vertices.Count == 0)
            return null;

        var min = new Vector3(float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity);
        foreach (var vertex in vertices)
        {
            var position = vertex.Position;
            if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z))
                return null;

            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new Aabb(min, max);
    }
}
```

On `Mesh`, store the box and fill it inside `Initialize`, before `Vertices.Clear()`:

```csharp
internal Aabb? Bounds { get; private set; }
```

```csharp
VertexCount = Vertices.Count;
Bounds = Aabb.FromPositions(Vertices);

Vertices.Clear();
```

In `MeshFactory.CreateCube`, replace the local `0.5f` with the shared constant:

```csharp
var size = Aabb.UnitCubeHalfExtent;
```

## 2. Planes and the corner test

Add `Engine/Renderer/Frustum.cs`. `Vector4.Transform` is a row vector times the matrix, so plane coefficients are columns of `M`. Near is `clip.z >= 0`. Far is `clip.w - clip.z >= 0`. The same six formulas fit the perspective camera and the light ortho.

```csharp
using System.Numerics;

namespace Engine.Renderer;

internal readonly struct Frustum
{
    private const float NormalEpsilon = 1e-8f;
    private const float OutsideEpsilon = 1e-4f;

    private readonly Plane[] _planes;

    private Frustum(Plane[] planes) => _planes = planes;

    public static bool TryFromClip(Matrix4x4 clip, out Frustum frustum)
    {
        frustum = default;
        var planes = new Plane[6];
        if (!TryPlane(clip.M11 + clip.M14, clip.M21 + clip.M24, clip.M31 + clip.M34, clip.M41 + clip.M44, out planes[0])
            || !TryPlane(clip.M14 - clip.M11, clip.M24 - clip.M21, clip.M34 - clip.M31, clip.M44 - clip.M41, out planes[1])
            || !TryPlane(clip.M12 + clip.M14, clip.M22 + clip.M24, clip.M32 + clip.M34, clip.M42 + clip.M44, out planes[2])
            || !TryPlane(clip.M14 - clip.M12, clip.M24 - clip.M22, clip.M34 - clip.M32, clip.M44 - clip.M42, out planes[3])
            || !TryPlane(clip.M13, clip.M23, clip.M33, clip.M43, out planes[4])
            || !TryPlane(clip.M14 - clip.M13, clip.M24 - clip.M23, clip.M34 - clip.M33, clip.M44 - clip.M43, out planes[5]))
            return false;

        frustum = new Frustum(planes);
        return true;
    }

    public bool IsOutside(Matrix4x4 world, Aabb bounds)
    {
        Span<Vector3> corners = stackalloc Vector3[8];
        var corner = 0;
        for (var z = 0; z < 2; z++)
        for (var y = 0; y < 2; y++)
        for (var x = 0; x < 2; x++)
        {
            var local = new Vector3(
                x == 0 ? bounds.Min.X : bounds.Max.X,
                y == 0 ? bounds.Min.Y : bounds.Max.Y,
                z == 0 ? bounds.Min.Z : bounds.Max.Z);
            var transformed = Vector3.Transform(local, world);
            if (!float.IsFinite(transformed.X) || !float.IsFinite(transformed.Y) || !float.IsFinite(transformed.Z))
                return false;

            corners[corner++] = transformed;
        }

        foreach (var plane in _planes)
        {
            var outside = true;
            foreach (var point in corners)
            {
                if (Plane.DotCoordinate(plane, point) >= -OutsideEpsilon)
                {
                    outside = false;
                    break;
                }
            }

            if (outside)
                return true;
        }

        return false;
    }

    private static bool TryPlane(float x, float y, float z, float d, out Plane plane)
    {
        var length = MathF.Sqrt(x * x + y * y + z * z);
        if (length <= NormalEpsilon)
        {
            plane = default;
            return false;
        }

        var inverse = 1f / length;
        plane = new Plane(new Vector3(x * inverse, y * inverse, z * inverse), d * inverse);
        return true;
    }
}
```

`OutsideEpsilon` is applied after the normal is unit length, so the slack is in world units. A corner a fraction of a millimeter outside still counts as inside. That is the anti-popping bias.

## 3. Wire it into the opaque walk

`PassStats` gains a reject count:

```csharp
public int Culled;
```

`DrawOpaque3D` takes the pass matrix. Both call sites pass the matrix they already have:

```csharp
shadow = DrawOpaque3D(
    context, graphics3D, textureFactory, modelFactory,
    meshDrawCounts: null, lightViewProjection);

var color = DrawOpaque3D(
    context, graphics3D, textureFactory, modelFactory,
    meshDrawCounts: logFrame ? MeshDrawCounts : null, view.ViewProjection);
```

```csharp
private static PassStats DrawOpaque3D(
    Context context,
    IGraphics3D graphics3D,
    ITextureFactory textureFactory,
    IModelFactory? modelFactory,
    Dictionary<Mesh, int>? meshDrawCounts,
    Matrix4x4 cullMatrix)
{
    var stats = new PassStats();
    var hasFrustum = Frustum.TryFromClip(cullMatrix, out var frustum);
    // existing loop
}
```

Helper used at every draw site. A missing box does not call it, so an uninitialized mesh still draws:

```csharp
private static bool IsCulled(bool hasFrustum, in Frustum frustum, Matrix4x4 world, Aabb bounds, ref PassStats stats)
{
    if (!hasFrustum || !frustum.IsOutside(world, bounds))
        return false;

    stats.Culled++;
    return true;
}
```

Plain cube, textured cube, and the fallback cube each check the unit box before drawing and before `CubeDraws` is incremented. One check covers the textured and plain branches, so a rejected cube does not load a texture. The fallback cube when a model fails to load gets its own check. Do not apply the model's scene-graph matrices.

```csharp
if (string.IsNullOrWhiteSpace(modelRenderer.ModelPath))
{
    if (IsCulled(hasFrustum, frustum, transform, Aabb.UnitCube, ref stats))
        continue;

    var factors = ResolvePbr(cube: true, modelRenderer, 0f, 0.5f);
    if (!string.IsNullOrWhiteSpace(modelRenderer.TexturePath))
        DrawCubeWithTexture(graphics3D, textureFactory, modelRenderer, transform, entity, factors);
    else
        graphics3D.DrawCube(transform, modelRenderer.Color, entity.Id,
            metallic: factors.Metallic, roughness: factors.Roughness, ao: factors.Ao);
    stats.CubeDraws++;
    continue;
}
```

```csharp
if (IsCulled(hasFrustum, frustum, transform, Aabb.UnitCube, ref stats))
    continue;

graphics3D.DrawCube(transform, tint, entity.Id,
    metallic: fallback.Metallic, roughness: fallback.Roughness, ao: fallback.Ao);
stats.CubeDraws++;
```

A selected submesh:

```csharp
if (meshIndex >= 0 && meshIndex < model.Submeshes.Count)
{
    var submesh = model.Submeshes[meshIndex];
    if (submesh.Bounds is { } bounds && IsCulled(hasFrustum, frustum, transform, bounds, ref stats))
        continue;

    QueueSubmesh(modelRenderer, transform, tint, entity.Id, submesh, ref stats, meshDrawCounts);
}
```

Every submesh of an unfiltered model:

```csharp
foreach (var submesh in model.Submeshes)
{
    if (submesh.Bounds is { } bounds && IsCulled(hasFrustum, frustum, transform, bounds, ref stats))
        continue;

    QueueSubmesh(modelRenderer, transform, tint, entity.Id, submesh, ref stats, meshDrawCounts);
}
```

`SuppressDraw`, a bad `MeshIndex`, and a null model factory stay ahead of this test, as they are today.

Add `culled={Culled}` to both lines in `LogDrawStats`, shadow and color.

## 4. Tests

`Engine.Tests` already sees internal engine types. Follow `Shouldly` and `[Trait("Category", "Unit")]`.

### Plane math

`tests/Engine.Tests/Renderer/FrustumTests.cs`.

```csharp
private static Matrix4x4 Perspective()
{
    var view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.UnitY);
    var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 3f, 16f / 9f, 0.1f, 100f);
    return view * projection;
}

private static Matrix4x4 Ortho()
{
    var view = Matrix4x4.CreateLookAt(new Vector3(0f, 0f, 5f), Vector3.Zero, Vector3.UnitY);
    var projection = Matrix4x4.CreateOrthographicOffCenter(-1f, 1f, -1f, 1f, 0.1f, 100f);
    return view * projection;
}
```

| Test | Setup | Assert |
|------|--------|--------|
| Inside | `TryFromClip(Perspective())`, unit cube at identity | `IsOutside` false |
| Left of ortho | Ortho, unit cube translated by `(3, 0, 0)` | true |
| Beyond far | Perspective, unit cube translated by `(0, 0, -200)` | true |
| Crossing the near side | Perspective, unit cube translated by `(0, 0, 4.9)`. The camera sits at z = 5 and the near plane is 0.1 in front of it, so the cube straddles that plane | false |
| Rotated rod misses | Ortho. Local box (−5, −0.02, −0.02)–(5, 0.02, 0.02). World matrix `CreateRotationZ(π/4) * CreateTranslation(6, 0, 0)`. The same rod at `(3, -3)` sits in the outside corner and stays, because no one plane holds every corner | true |
| Rotated rod hits | Same rod at identity | false |
| Negative scale | `CreateScale(-1, -1, -1)`, unit cube, perspective | false |
| Non-finite | Translation `(NaN, 0, 0)` | false |
| No planes | `TryFromClip(default)` | false, frustum unused |

The rod shifted to x = 6 is wholly past the right face after the 45° turn. The same rod at `(3, -3)` misses the square through the outside corner, and this test keeps it. That is the accepted extra draw.

### Box survives upload

Extend `MeshCpuDataReleaseTests`. After `Initialize` on a triangle whose positions are `(0,0,0)`, `(1,0,0)`, `(0,2,0)`:

```csharp
mesh.Vertices.ShouldBeEmpty();
mesh.Bounds.ShouldBe(new Aabb(Vector3.Zero, new Vector3(1f, 2f, 0f)));
```

A second fact: `Initialize` on a mesh with no vertices leaves `Bounds` null. `Aabb.UnitCube` is min −0.5 and max 0.5 on every axis.

### Pipeline

Add facts to `SceneRenderPipelineShadowTests`. Move objects with `SetWorldTransform`. Assigning `Translation` leaves `GetWorldTransform` at identity, and the walk reads the world matrix.

Keep `RenderScene_DirectionalLight_DrawsCubeInBothPasses`. The cube at the origin is inside that camera. It must still draw twice.

**Outside both.** Directional light on. `SetWorldTransform(Matrix4x4.CreateTranslation(1000f, 0f, 0f))`. `CubeDraws` is 0. `ShadowPasses` still has one matrix. `Order` contains `begin-shadow` and `begin-scene` and no `cube`.

**Color only, past the shadow distance.** Eye and forward match `ViewProjection` in that file. `ShadowDistance` is 50 and that helper's far plane is 100.

```csharp
var eye = new Vector3(0f, 2f, 5f);
var forward = Vector3.Normalize(new Vector3(0f, -0.3f, -1f));
transform.SetWorldTransform(Matrix4x4.CreateTranslation(eye + forward * 80f));
```

One cube, and it is the color draw: `Order` is `shadow-off`, `begin-shadow`, `end-shadow`, `shadow-on`, `begin-scene`, `cube`.

**Shadow only.** Fit the light matrix the same way the existing test does. Invert it and unproject clip `(0.95, 0, 0.5)` with the same 0-at-near convention as `LightingMath`. Place the unit cube on that world point with `SetWorldTransform`. Before asserting draws, require the camera frustum to report the unit cube outside and the light frustum to report it inside. Then `Order` contains one `cube`, and that `cube` sits between `begin-shadow` and `end-shadow`.

If that clip point ever fails the frustum precondition, the test fails there. Do not weaken it into "any single cube draw."

**One submesh of two.** Build two meshes, `Initialize` both with the substitute factories from `MeshCpuDataReleaseTests`, and put them on one `Model`. The near mesh is a triangle around the origin. The far mesh is a triangle around `(1000, 0, 0)`. Entity world matrix stays identity. With a directional light, `MeshInstanceCounts` sums to 2: the near mesh once in shadow and once in color. The far mesh is absent. An uninitialized mesh with `Bounds == null` is covered by the existing prop tests, which must still draw.

No new sprite tests.
