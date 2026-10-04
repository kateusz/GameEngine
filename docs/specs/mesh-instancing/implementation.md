# Mesh Instancing — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL replaces the model and depth vertex shaders' per-draw uniforms. Cube shaders stay as they are.

The vertex-buffer cap is **256 MB**, shared with `OpenGLVertexBuffer`. The color record is **160** bytes. The shadow record is one transposed matrix, **64** bytes. Attribute locations **0–15** are the whole budget. Do not append a field: a GPU with sixteen attributes drops location 16 and the picture loses that value with no shader error. A second 4×4 matrix does not fit, so the normal transform is three columns. Shadow and color use separate instance buffers. Every write orphans the store first.

## 1. Record layout

Replace `Engine/Renderer/MeshInstanceData.cs`.

```csharp
using System.Numerics;
using System.Runtime.InteropServices;

namespace Engine.Renderer;

/// <summary>
/// One shaded copy of a shared mesh. Matrices are columns, matching
/// <c>glUniformMatrix</c> with <c>transpose = true</c>. The shader still multiplies <c>vec4 * mat4</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct MeshInstanceData
{
    public Matrix4x4 Model;
    public Vector4 NormalX;
    public Vector4 NormalY;
    public Vector4 NormalZ;
    public int EntityId;
    public int Pad0;
    public int Pad1;
    public int Pad2;
    public Vector4 Color;
    public Vector4 Pbr;

    public const int ByteSize = 160;
    public const int ShadowByteSize = 64;

    public const int ModelLocation = 6;
    public const int NormalLocation = 10;
    public const int EntityIdLocation = 13;
    public const int ColorLocation = 14;
    public const int PbrLocation = 15;

    public const int NormalXOffset = 64;
    public const int NormalYOffset = 80;
    public const int NormalZOffset = 96;
    public const int EntityIdOffset = 112;
    public const int ColorOffset = 128;
    public const int PbrOffset = 144;

    public static MeshInstanceData Create(
        Matrix4x4 world, Vector4 color, int entityId, float metallic, float roughness, float ao)
    {
        var normal = NormalMatrix(world);
        return new MeshInstanceData
        {
            Model = Matrix4x4.Transpose(world),
            NormalX = new Vector4(normal.M11, normal.M21, normal.M31, 0f),
            NormalY = new Vector4(normal.M12, normal.M22, normal.M32, 0f),
            NormalZ = new Vector4(normal.M13, normal.M23, normal.M33, 0f),
            EntityId = entityId,
            Color = color,
            Pbr = new Vector4(metallic, roughness, ao, 0f)
        };
    }

    public static Matrix4x4 NormalMatrix(Matrix4x4 model) =>
        Matrix4x4.Invert(model, out var inverse) ? Matrix4x4.Transpose(inverse) : Matrix4x4.Identity;

    public static int SliceCount(int copies, int stride)
    {
        if (copies <= 0 || stride <= 0)
            return 0;

        var max = (int)(RenderingConstants.MaxVertexBufferBytes / (uint)stride);
        return max == 0 ? 0 : (copies + max - 1) / max;
    }
}
```

Move the private 256 MB cap in `OpenGLVertexBuffer` onto `RenderingConstants` and use it from both sites:

```csharp
public const uint MaxVertexBufferBytes = 256 * 1024 * 1024;
```

On `Mesh`, skip a mesh the GPU cannot draw:

```csharp
internal bool CanDraw => _initialized && GetIndexCount() > 0;
```

## 2. Instance attributes and the instanced draw

`IRendererAPI`:

```csharp
void DrawIndexedInstanced(IVertexArray vertexArray, uint indexCount, uint instanceCount);
```

`OpenGLRendererApi`, same binding rule as `DrawIndexed` (the caller has bound the vertex array):

```csharp
public unsafe void DrawIndexedInstanced(IVertexArray vertexArray, uint indexCount, uint instanceCount)
{
    var itemsCount = indexCount != 0 ? indexCount : (uint)vertexArray.IndexBuffer.Count;
    SilkNetContext.GL.DrawElementsInstanced(
        PrimitiveType.Triangles, itemsCount, DrawElementsType.UnsignedInt, (void*)0, instanceCount);
    OpenGLDebug.CheckError(SilkNetContext.GL, "DrawElementsInstanced");
}
```

`IVertexBuffer` gains a prefix rewrite. Remember the allocated size. `BufferSubData` cannot grow the store, and it stalls when a draw is still reading that store. Orphan first: `BufferData` of the same size with a null pointer allocates a new store and leaves the old one with in-flight draws. Then write the prefix. The caller guarantees `data.Length` fits the buffer created with `Create(uint)`.

```csharp
void Rewrite(ReadOnlySpan<byte> data);
```

```csharp
private uint _capacity;

public void Rewrite(ReadOnlySpan<byte> data)
{
    ObjectDisposedException.ThrowIf(_disposed, this);
    if (data.IsEmpty)
        return;
    if ((uint)data.Length > _capacity)
        throw new ArgumentException("Rewrite exceeds the allocated store.", nameof(data));

    SilkNetContext.GL.BindBuffer(GLEnum.ArrayBuffer, RendererId);
    unsafe
    {
        SilkNetContext.GL.BufferData(BufferTargetARB.ArrayBuffer, _capacity, (void*)null, BufferUsageARB.DynamicDraw);
        OpenGLDebug.CheckError(SilkNetContext.GL, "BufferData(orphan)");
        fixed (byte* pointer = data)
            SilkNetContext.GL.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)data.Length, pointer);
    }

    OpenGLDebug.CheckError(SilkNetContext.GL, "BufferSubData");
}
```

The 2D `SetData` overloads stay as they are. Only the instance path orphans. Set `_capacity = size` as the first line of the existing `Allocate`.

Do not send instance attributes through `AddVertexBuffer`. That path numbers locations from 0 and treats a matrix as one attribute. `IVertexArray`:

```csharp
void SetInstanceAttributes(IVertexBuffer buffer, int firstByte, int stride, bool withMaterial);
```

`OpenGLVertexArray` binds the mesh vertex array, then the instance buffer, and sets divisor 1. `withMaterial` false is the shadow path: slots 10–15 are disabled so a previous color draw cannot fetch them with the 64-byte stride.

```csharp
public unsafe void SetInstanceAttributes(IVertexBuffer buffer, int firstByte, int stride, bool withMaterial)
{
    Bind();
    buffer.Bind();
    SetMatrix(MeshInstanceData.ModelLocation, firstByte, stride);
    if (!withMaterial)
    {
        Disable(MeshInstanceData.NormalLocation, MeshInstanceData.PbrLocation);
        return;
    }

    // mat3 columns are vec3. The record still stores a vec4 slot so the next field stays aligned.
    SetVec3(MeshInstanceData.NormalLocation, firstByte + MeshInstanceData.NormalXOffset, stride);
    SetVec3((uint)(MeshInstanceData.NormalLocation + 1), firstByte + MeshInstanceData.NormalYOffset, stride);
    SetVec3((uint)(MeshInstanceData.NormalLocation + 2), firstByte + MeshInstanceData.NormalZOffset, stride);
    SetInt(MeshInstanceData.EntityIdLocation, firstByte + MeshInstanceData.EntityIdOffset, stride);
    SetVec4(MeshInstanceData.ColorLocation, firstByte + MeshInstanceData.ColorOffset, stride);
    SetVec4(MeshInstanceData.PbrLocation, firstByte + MeshInstanceData.PbrOffset, stride);
}

private unsafe void SetMatrix(uint location, int offset, int stride)
{
    for (var column = 0; column < 4; column++)
        SetVec4(location + (uint)column, offset + column * 16, stride);
}

private unsafe void SetVec4(uint location, int offset, int stride) =>
    SetFloats(location, 4, offset, stride);

private unsafe void SetVec3(uint location, int offset, int stride) =>
    SetFloats(location, 3, offset, stride);

private unsafe void SetFloats(uint location, int components, int offset, int stride)
{
    SilkNetContext.GL.EnableVertexAttribArray(location);
    SilkNetContext.GL.VertexAttribPointer(
        location, components, VertexAttribPointerType.Float, false, (uint)stride, (void*)offset);
    SilkNetContext.GL.VertexAttribDivisor(location, 1);
    OpenGLDebug.CheckError(SilkNetContext.GL, $"VertexAttribPointer({location})");
}

private unsafe void SetInt(uint location, int offset, int stride)
{
    SilkNetContext.GL.EnableVertexAttribArray(location);
    SilkNetContext.GL.VertexAttribIPointer(location, 1, GLEnum.Int, (uint)stride, (void*)offset);
    SilkNetContext.GL.VertexAttribDivisor(location, 1);
    OpenGLDebug.CheckError(SilkNetContext.GL, $"VertexAttribIPointer({location})");
}

private void Disable(int from, int through)
{
    for (var location = from; location <= through; location++)
        SilkNetContext.GL.DisableVertexAttribArray((uint)location);
}
```

## 3. Shaders

`modelShader.vert` — drop `u_Model`, `u_NormalMatrix`, and the mesh `a_EntityID` read. Location 5 stays in the mesh layout and is unused here.

```glsl
#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 1) in vec3 a_Normal;
layout(location = 2) in vec2 a_TexCoord;
layout(location = 3) in vec3 a_Tangent;
layout(location = 4) in vec3 a_Bitangent;

layout(location = 6) in mat4 a_Model;
layout(location = 10) in mat3 a_NormalMatrix;
layout(location = 13) in int a_InstanceEntityId;
layout(location = 14) in vec4 a_InstanceColor;
layout(location = 15) in vec4 a_InstancePbr;

uniform mat4 u_ViewProjection;

out vec3 v_FragPos;
out vec3 v_Normal;
out vec2 v_TexCoord;
out mat3 v_TBN;
flat out int v_EntityID;
flat out vec4 v_Color;
flat out vec4 v_Pbr;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * a_Model;
    v_FragPos = worldPos.xyz;
    v_Normal = normalize(a_Normal * a_NormalMatrix);
    v_TexCoord = a_TexCoord;
    v_EntityID = a_InstanceEntityId;
    v_Color = a_InstanceColor;
    v_Pbr = a_InstancePbr;

    vec3 T = normalize(a_Tangent * a_NormalMatrix);
    vec3 N = v_Normal;
    T = normalize(T - dot(T, N) * N);
    vec3 B = cross(N, T);
    v_TBN = mat3(T, B, N);

    gl_Position = worldPos * u_ViewProjection;
}
```

`modelShader.frag` — delete `u_Color`, `u_EntityID`, `u_Metallic`, `u_Roughness`, and `u_Ao`. Add the flat inputs next to `v_EntityID`. Keep `u_BaseColor` and the map flags.

```glsl
flat in int v_EntityID;
flat in vec4 v_Color;
flat in vec4 v_Pbr;
```

```glsl
vec3 albedo = (u_HasDiffuseMap != 0 ? texture(u_DiffuseMap, v_TexCoord).rgb : vec3(1.0))
    * u_BaseColor * v_Color.rgb;
vec3 mr = u_HasMetallicRoughnessMap != 0
    ? texture(u_MetallicRoughnessMap, v_TexCoord).rgb
    : vec3(1.0);
float metallic = clamp(mr.b * v_Pbr.x, 0.0, 1.0);
float roughness = max(clamp(mr.g * v_Pbr.y, 0.0, 1.0), c_MinRoughness);
float aoSample = u_HasOcclusionMap != 0 ? texture(u_OcclusionMap, v_TexCoord).r : 1.0;
float ao = clamp(aoSample * v_Pbr.z, 0.0, 1.0);
```

```glsl
o_Color = vec4(Encode(ambient + sun + lamps), v_Color.a);
o_EntityID = v_EntityID;
```

`depth.vert`:

```glsl
#version 330 core

layout(location = 0) in vec3 a_Position;
layout(location = 6) in mat4 a_Model;

uniform mat4 u_ViewProjection;

void main()
{
    vec4 worldPos = vec4(a_Position, 1.0) * a_Model;
    gl_Position = worldPos * u_ViewProjection;
}
```

## 4. Flush from graphics

Replace `DrawMesh` on `IGraphics3D` with two flushes. Each returns how many slices were submitted.

```csharp
int DrawColorMeshes(IReadOnlyDictionary<Mesh, List<MeshInstanceData>> batches);
int DrawShadowMeshes(IReadOnlyDictionary<Mesh, List<Matrix4x4>> batches);
```

Add `IVertexBufferFactory` to the `Graphics3D` constructor. DryIoc already registers that factory, so the container line stays. `Graphics3DDisposeTests` passes `Substitute.For<IVertexBufferFactory>()`.

Delete `DrawMesh`, `DrawShadow`, and `ComputeNormalMatrix`. `BindCommon` for cubes calls `MeshInstanceData.NormalMatrix`.

Fields:

```csharp
private readonly IVertexBufferFactory _vertexBuffers;
private IVertexBuffer? _colorInstances;
private IVertexBuffer? _shadowInstances;
private int _colorBytes;
private int _shadowBytes;
private MeshInstanceData[] _colorScratch = [];
private Matrix4x4[] _shadowScratch = [];
private readonly List<InstanceSlice> _slices = [];

private readonly record struct InstanceSlice(Mesh Mesh, int Start, int Count);
```

`Dispose` disposes both instance buffers before dropping the shader references.

`BeginShadowPass` has already bound the depth shader. The shadow flush must not bind or unbind it. The color flush follows and binds the model shader itself.

```csharp
public int DrawShadowMeshes(IReadOnlyDictionary<Mesh, List<Matrix4x4>> batches)
{
    var draws = 0;
    var used = 0;
    var maxCopies = (int)(RenderingConstants.MaxVertexBufferBytes / MeshInstanceData.ShadowByteSize);
    _slices.Clear();

    foreach (var (mesh, list) in batches)
    {
        if (list.Count == 0 || !mesh.CanDraw)
            continue;

        var offset = 0;
        while (offset < list.Count)
        {
            var count = Math.Min(list.Count - offset, maxCopies);
            if (used > 0 && used + count > maxCopies)
            {
                draws += FlushShadow(used);
                used = 0;
            }

            EnsureScratch(ref _shadowScratch, used + count);
            list.CopyTo(offset, _shadowScratch, used, count);
            _slices.Add(new InstanceSlice(mesh, used, count));
            used += count;
            offset += count;
        }
    }

    if (used > 0)
        draws += FlushShadow(used);

    return draws;
}

private int FlushShadow(int used)
{
    Upload(_shadowScratch.AsSpan(0, used), ref _shadowInstances, ref _shadowBytes);
    var draws = 0;
    foreach (var slice in _slices)
    {
        slice.Mesh.Bind();
        slice.Mesh.GetVertexArray().SetInstanceAttributes(
            _shadowInstances!,
            slice.Start * MeshInstanceData.ShadowByteSize,
            MeshInstanceData.ShadowByteSize,
            withMaterial: false);
        rendererApi.DrawIndexedInstanced(
            slice.Mesh.GetVertexArray(), (uint)slice.Mesh.GetIndexCount(), (uint)slice.Count);
        _stats.DrawCalls++;
        draws++;
    }

    _slices.Clear();
    return draws;
}
```

```csharp
public int DrawColorMeshes(IReadOnlyDictionary<Mesh, List<MeshInstanceData>> batches)
{
    var draws = 0;
    var used = 0;
    var maxCopies = (int)(RenderingConstants.MaxVertexBufferBytes / MeshInstanceData.ByteSize);
    _slices.Clear();

    foreach (var (mesh, list) in batches)
    {
        if (list.Count == 0 || !mesh.CanDraw)
            continue;

        var offset = 0;
        while (offset < list.Count)
        {
            var count = Math.Min(list.Count - offset, maxCopies);
            if (used > 0 && used + count > maxCopies)
            {
                draws += FlushColor(used);
                used = 0;
            }

            EnsureScratch(ref _colorScratch, used + count);
            list.CopyTo(offset, _colorScratch, used, count);
            _slices.Add(new InstanceSlice(mesh, used, count));
            used += count;
            offset += count;
        }
    }

    if (used > 0)
        draws += FlushColor(used);

    return draws;
}

private int FlushColor(int used)
{
    Upload(_colorScratch.AsSpan(0, used), ref _colorInstances, ref _colorBytes);
    _modelShader.Bind();
    var draws = 0;
    foreach (var slice in _slices)
    {
        BindMeshMaterial(_modelShader, slice.Mesh);
        slice.Mesh.Bind();
        slice.Mesh.GetVertexArray().SetInstanceAttributes(
            _colorInstances!,
            slice.Start * MeshInstanceData.ByteSize,
            MeshInstanceData.ByteSize,
            withMaterial: true);
        rendererApi.DrawIndexedInstanced(
            slice.Mesh.GetVertexArray(), (uint)slice.Mesh.GetIndexCount(), (uint)slice.Count);
        _stats.DrawCalls++;
        draws++;
    }

    _modelShader.Unbind();
    _slices.Clear();
    return draws;
}

private void Upload<T>(Span<T> records, ref IVertexBuffer? buffer, ref int capacity) where T : struct
{
    var bytes = MemoryMarshal.AsBytes(records);
    EnsureInstanceBuffer(ref buffer, ref capacity, bytes.Length);
    buffer!.Rewrite(bytes);
}

private void EnsureInstanceBuffer(ref IVertexBuffer? buffer, ref int capacity, int bytes)
{
    if (bytes <= capacity)
        return;

    var grown = capacity == 0 ? bytes : Math.Max(bytes, capacity * 2);
    var cap = (int)RenderingConstants.MaxVertexBufferBytes;
    if (grown > cap)
        grown = cap;
    if (grown < bytes)
        throw new InvalidOperationException("Instance slice exceeds the vertex buffer cap.");

    buffer?.Dispose();
    buffer = _vertexBuffers.Create((uint)grown);
    capacity = grown;
}

private static void EnsureScratch<T>(ref T[] scratch, int needed)
{
    if (scratch.Length >= needed)
        return;

    var size = scratch.Length == 0 ? needed : Math.Max(needed, scratch.Length * 2);
    Array.Resize(ref scratch, size);
}
```

`BindMeshMaterial` is the texture and map-flag block from today's `DrawMesh`, without `u_Model`, `u_Color`, `u_EntityID`, or the three factors. Those are in the record. Call it once per slice.

A slice length is at most `maxCopies`, so one slice never asks `EnsureInstanceBuffer` for more than the cap. A pass that does not fit is several orphan-and-writes on that pass's buffer. Copies are not dropped. Growing deletes the old buffer object. OpenGL keeps its store until the draws that captured it finish. Do not write into that store with `BufferSubData` instead.

## 5. Gather in the opaque walk

Two static dictionaries on `SceneRenderPipeline`. Clear the pass's lists at the start of `DrawOpaque3D`. Pass `shadow: true` from the shadow call and `shadow: false` from the color call.

```csharp
private static readonly Dictionary<Mesh, List<MeshInstanceData>> ColorBatches = new();
private static readonly Dictionary<Mesh, List<Matrix4x4>> ShadowBatches = new();
private static readonly List<Mesh> PruneKeys = [];
```

```csharp
private struct PassStats
{
    public int Renderers;
    public int Instances;
    public int MeshDraws;
    public int CubeDraws;
    public int Culled;
    public int MissingMeshIndex;
    public long Vertices;
    public long Indices;
    public double CpuMs;
}
```

Replace the body of `DrawSubmesh` with an append. The cull checks above it stay. Do not append when `!submesh.CanDraw`.

```csharp
private static void AppendSubmesh(
    ModelRendererComponent modelRenderer,
    Matrix4x4 transform,
    Vector4 tint,
    int entityId,
    Mesh submesh,
    bool shadow,
    ref PassStats stats,
    Dictionary<Mesh, int>? meshDrawCounts)
{
    if (!submesh.CanDraw)
        return;

    stats.Instances++;
    stats.Vertices += submesh.VertexCount;
    stats.Indices += submesh.GetIndexCount();

    if (shadow)
    {
        BatchList(ShadowBatches, submesh).Add(Matrix4x4.Transpose(transform));
        return;
    }

    if (meshDrawCounts != null)
    {
        meshDrawCounts.TryGetValue(submesh, out var count);
        meshDrawCounts[submesh] = count + 1;
    }

    var pbr = ResolvePbr(cube: false, modelRenderer, submesh.MetallicFactor, submesh.RoughnessFactor);
    BatchList(ColorBatches, submesh).Add(
        MeshInstanceData.Create(transform, tint, entityId, pbr.Metallic, pbr.Roughness, pbr.Ao));
}
```

After the entity loop, before the CPU timestamp:

```csharp
stats.MeshDraws += shadow
    ? graphics3D.DrawShadowMeshes(ShadowBatches)
    : graphics3D.DrawColorMeshes(ColorBatches);
if (shadow)
    Prune(ShadowBatches);
else
    Prune(ColorBatches);
```

```csharp
private static void ClearBatches<T>(Dictionary<Mesh, List<T>> batches)
{
    foreach (var list in batches.Values)
        list.Clear();
}

private static List<T> BatchList<T>(Dictionary<Mesh, List<T>> batches, Mesh mesh)
{
    if (!batches.TryGetValue(mesh, out var list))
        batches[mesh] = list = [];
    return list;
}

private static void Prune<T>(Dictionary<Mesh, List<T>> batches)
{
    PruneKeys.Clear();
    foreach (var pair in batches)
    {
        if (pair.Value.Count == 0)
            PruneKeys.Add(pair.Key);
    }

    foreach (var mesh in PruneKeys)
        batches.Remove(mesh);
}
```

Call `ClearBatches` on the pass's dictionary at the start of `DrawOpaque3D`, before the entity loop.

Log lines gain `instances` and keep `meshDraws` as the slice count. The repeated-mesh line:

```csharp
var draws = MeshInstanceData.SliceCount(pair.Value, MeshInstanceData.ByteSize);
Logger.Information(
    "3D repeated mesh instances={Instances} draws={Draws} vertices={Vertices} indices={Indices} triangles={Triangles} submittedVertices={SubmittedVertices} name={Name}",
    pair.Value, draws, mesh.VertexCount, indexCount, indexCount / 3,
    (long)pair.Value * mesh.VertexCount, mesh.Name);
```

The early-out that skips a quiet frame already treats `MeshDraws == 0` as "nothing submitted". Slice count satisfies that. `Culled` is unchanged.

## 6. Tests

`RecordingGraphics3D` drops `DrawMesh`. Each flush appends `"mesh"` once per slice, so the existing far-submesh order stays `begin-shadow`, `mesh`, `end-shadow`, …, `begin-scene`, `mesh`. Copy the records out of the lists. The walk clears those lists on the next pass.

```csharp
public List<MeshInstanceData[]> ColorRecords { get; } = [];
public List<Matrix4x4[]> ShadowRecords { get; } = [];

public int DrawColorMeshes(IReadOnlyDictionary<Mesh, List<MeshInstanceData>> batches)
{
    return Record(batches, ColorRecords, MeshInstanceData.ByteSize, list => list.ToArray());
}

public int DrawShadowMeshes(IReadOnlyDictionary<Mesh, List<Matrix4x4>> batches)
{
    return Record(batches, ShadowRecords, MeshInstanceData.ShadowByteSize, list => list.ToArray());
}

private int Record<T>(
    IReadOnlyDictionary<Mesh, List<T>> batches,
    List<T[]> sink,
    int stride,
    Func<List<T>, T[]> copy)
{
    var draws = 0;
    foreach (var (mesh, list) in batches)
    {
        if (list.Count == 0 || !mesh.CanDraw)
            continue;

        var slices = MeshInstanceData.SliceCount(list.Count, stride);
        draws += slices;
        MeshDraws += slices;
        for (var i = 0; i < slices; i++)
            Order.Add("mesh");
        sink.Add(copy(list));
    }

    return draws;
}
```

Add facts beside that double:

- Two entities, one mesh, different `Color` and `OverrideMaterial`: one color array of length 2, both tints and both `Pbr` values present, both `EntityId`s present. One shadow array of length 2 whose matrices equal `Matrix4x4.Transpose` of each world matrix. `MeshDraws` is 2 (one slice per pass).
- Two meshes: two color arrays, one record each. Assert the count, not dictionary order.
- Two cubes: `CubeDraws` is 2 and `ColorRecords` is empty.
- `SuppressDraw`, a bad mesh index, and a failed model: no color record. The failed model still draws the fallback cube.
- A mesh with `CanDraw` false next to a triangle that can draw: only the triangle is recorded.
- `MeshInstanceData.Create` with a non-finite translation still returns a record.

Pure checks, no scene:

```csharp
Unsafe.SizeOf<MeshInstanceData>().ShouldBe(MeshInstanceData.ByteSize);
Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Color)).ToInt32().ShouldBe(MeshInstanceData.ColorOffset);
Marshal.OffsetOf<MeshInstanceData>(nameof(MeshInstanceData.Pbr)).ToInt32().ShouldBe(MeshInstanceData.PbrOffset);

var max = (int)(RenderingConstants.MaxVertexBufferBytes / MeshInstanceData.ByteSize);
MeshInstanceData.SliceCount(max, MeshInstanceData.ByteSize).ShouldBe(1);
MeshInstanceData.SliceCount(max + 1, MeshInstanceData.ByteSize).ShouldBe(2);
MeshInstanceData.SliceCount(0, MeshInstanceData.ByteSize).ShouldBe(0);
```

Do not build a list of `max + 1` records. Sprite tests stay as they are.
