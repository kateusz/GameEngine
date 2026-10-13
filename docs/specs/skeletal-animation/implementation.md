# Skeletal Animation — Implementation

C# sketch for the design in `introduction.md` and `developer-guide.md`. Do not add blend trees, root motion, morph targets, or a second skin. The draw coordinator only binds a palette. Sampling stays in the pose update.

Format version becomes **2**. Importer revision becomes **8**. Joint cap is **128**. Influences per vertex are **4**. The joint uniform block is binding point **0**, 64 bytes a matrix, std140.

## 1. Playback component

`SceneComponents/Rendering/SkinnedAnimationComponent.cs`

Why: the scene file keeps the clip choice. The clock and the palette are runtime state, same as an audio source that does not save its cursor.

```csharp
using System.Numerics;
using System.Text.Json.Serialization;
using ECS;

namespace SceneComponents.Rendering;

public sealed class SkinnedAnimationComponent : IComponent
{
    public const int MaxJoints = 128;

    private string _clipName = "";

    public string ClipName
    {
        get => _clipName;
        set
        {
            value ??= "";
            if (_clipName == value)
                return;
            _clipName = value;
            Time = 0f;
            LastTime = 0f;
            PoseReady = false;
            if (KeyCursors.Length > 0)
                Array.Clear(KeyCursors);
        }
    }

    public bool Loop { get; set; } = true;

    [JsonIgnore] public float Time { get; set; }
    [JsonIgnore] public float LastTime { get; set; }
    [JsonIgnore] public bool PoseReady { get; set; }
    [JsonIgnore] public Matrix4x4[] Pose { get; private set; } = [];
    [JsonIgnore] public Matrix4x4[] Locals { get; private set; } = [];
    [JsonIgnore] public int[] KeyCursors { get; private set; } = [];

    public void EnsureCapacity(int jointCount)
    {
        if (jointCount == Pose.Length)
            return;
        Pose = new Matrix4x4[jointCount];
        Locals = new Matrix4x4[jointCount];
        KeyCursors = new int[jointCount * 3];
        PoseReady = false;
    }

    public IComponent Clone() => new SkinnedAnimationComponent
    {
        _clipName = _clipName,
        Loop = Loop
    };
}
```

Register it beside the other built-ins in `ComponentSerializerRegistry.RegisterBuiltins`:

```csharp
Register<SkinnedAnimationComponent>();
```

Import adds the component. There is no Add Component menu row.

## 2. Asset types

Why: clips and joints are model data. The entity only points at a clip name.

On `Model`:

```csharp
public SkinData? Skin { get; }
public Matrix4x4[] RestPose { get; }
```

`RestPose` is empty when `Skin` is null. Otherwise its length is the joint count, built once at load by the same walk as playback, using rest locals only.

```csharp
public sealed class SkinData
{
    public required JointData[] Joints { get; init; }
    public required ClipData[] Clips { get; init; }
}

public readonly struct JointData
{
    public required int Parent { get; init; } // -1 = root
    public required Matrix4x4 InverseBind { get; init; }
    public required Vector3 RestTranslation { get; init; }
    public required Quaternion RestRotation { get; init; }
    public required Vector3 RestScale { get; init; }
}

public sealed class ClipData
{
    public required string Name { get; init; }
    public required float Duration { get; init; }
    public required ClipChannel[] Channels { get; init; }
}

public readonly struct ClipChannel
{
    public required int Joint { get; init; }
    public required ChannelProperty Property { get; init; }
    public required ChannelInterpolation Interpolation { get; init; }
    public required float[] Times { get; init; }
    public required float[] Values { get; init; } // 3 floats, or 4 for rotation
}

public enum ChannelProperty { Translation = 1, Rotation = 2, Scale = 3 }
public enum ChannelInterpolation { Step = 1, Linear = 2 }
```

`Mesh` gains `bool IsSkinned`. A model with a skin sets it on every submesh, including vertices whose weights are zero, and uploads the skinned layout. The static `Mesh.Vertex` layout stays the one used by a model with no skin. Attribute locations follow element order, which is what `OpenGLVertexArray` already binds:

| Location | Element |
|----------|---------|
| 0–4 | Position, normal, uv, tangent, bitangent |
| 5 | Entity id (`Int`, `VertexAttribIPointer`) |
| 6 | Joints (`Int4`, `VertexAttribIPointer`) |
| 7 | Weights (`Float4`) |

## 3. Sibling version 2

Why: the player reads the sibling only. Version 1 has nowhere to put joints.

`RuntimeMeshFormat.Version = 2`, `ImporterVersion = 8`, `HeaderSize = 144`. Bytes 0–115 match the version-1 header through the light-table offset. Bytes 116–127 stay zero. Bytes 128–135 are the skin-table offset. Bytes 136–143 are the clip-table offset. Offset 0 with a missing table means empty.

Add `PostProcessSteps.LimitBoneWeights` to `AssimpPostProcessFlags`. The Assimp default cap is 4. `RuntimeMeshBuilder.TryEnsureUpToDate` already recooks when the importer revision or the flags disagree, and the reader accepts only version 2.

Skin table, little-endian, 8-byte aligned:

- `u32` joint count, `u32` zero
- Per joint: `i32` parent, 16 floats of inverse bind (translation in the fourth row), `float3` rest translation, `float4` rest rotation xyzw, `float3` rest scale

Clip table:

- `u32` clip count, `u32` zero
- Per clip: counted string (the existing string helper), `float` duration, `u32` channel count
- Per channel: `u32` joint, `u32` property, `u32` interpolation, `u32` key count, then keys of `float` time plus 3 or 4 floats

After each submesh's index list, version 2 writes `u32` 0 when the file has no skin. When the file has a skin, every submesh writes `u32` 1 and one weight record per vertex: four `u32` joint indices and four floats. A vertex with no influences stores zeros. The record byte length includes them.

`RuntimeMeshReader` rejects the file when the version is not 2, a count does not fit, a joint index is past the joint list, a parent index is not -1 and not in range, a weight is non-finite, or a required table offset is out of range. Rejection allocates no vertex buffer. `ModelFactory` then returns no meshes, and the opaque walk draws the stand-in cube. A version other than 2 also fails the stamp in `RuntimeMeshBuilder.TryEnsureUpToDate` once the importer revision is 8, so the editor cooks from the source. A version-2 file whose stamp matches and whose tables fail is not rewritten.

## 4. Import

Why: one Assimp path for glTF, GLB, and FBX. The cook drops a skin it cannot pose, and still writes the meshes.

In `AssimpModelImporter`, after the existing node walk:

- Read the first skin only. Log and ignore the rest.
- Map Assimp bones onto joints. Parent links come from the node graph. Offset matrices go through `ToEngineMatrix`.
- Convert each quaternion field by field: `new Quaternion(x, y, z, w)`. Assimp stores w first. A block copy swaps w and x.
- Decompose each rest local with `Matrix4x4.Decompose`. Failure drops the skin.
- More than 128 joints, a parent cycle, a parent outside the list, or a non-finite inverse bind drops the skin. Log it. Vertices are stored with no weight records.
- Store the weights Assimp returns after limit-bone-weights. That flag is the cap of four. A vertex with no influences stores zeros. A joint index past the list drops the skin.
- Clips: match channel names to joints, ordinal. Skip an unknown joint and a non-finite key. Two clips with the same name keep the first. Duration is the max key time, or 0 when nothing remains. Read cubic-spline keys as linear and ignore the tangents.

`ModelFactory` copies `SkinData` onto `Model` and fills `RestPose` by calling `SkinnedPose.BuildRest`.

## 5. Pose update

`Engine/Renderer/Models/SkinnedPose.cs`

Why: edit mode and play mode must share one walk. The walk uses the same multiply as node transforms: `local * parentGlobal`, then `inverseBind * global`.

```csharp
public static void Advance(Context context, IModelFactory models, TimeSpan delta)
{
    var dt = (float)delta.TotalSeconds;
    if (!float.IsFinite(dt) || dt < 0f)
        dt = 0f;

    foreach (var (entity, playback) in context.View<SkinnedAnimationComponent>())
    {
        if (!entity.TryGetComponent<ModelRendererComponent>(out var renderer)
            || string.IsNullOrWhiteSpace(renderer.ModelPath))
        {
            playback.PoseReady = false;
            continue;
        }

        var model = models.Create(PathBuilder.Resolve(renderer.ModelPath));
        if (model?.Skin is not { } skin)
        {
            playback.PoseReady = false;
            continue;
        }

        playback.EnsureCapacity(skin.Joints.Length);
        if (!AdvanceClock(playback, skin, dt))
        {
            playback.PoseReady = false;
            continue;
        }

        var visible = !entity.TryGetComponent<TransformComponent>(out var transform)
                      || transform.EffectiveVisible;
        if (!visible)
        {
            playback.PoseReady = false;
            continue;
        }

        SampleLocals(playback, skin, playback.Locals);
        BuildPalette(skin, playback.Locals, playback.Pose);
        playback.PoseReady = true;
    }
}
```

`models.Create` is the existing path cache. A hit does not parse the source and does not allocate a new model.

`AdvanceClock` resolves the clip by an ordinal scan of `skin.Clips`, with no LINQ. An empty name, a miss, or a duration of 0 sets `Time` and `LastTime` to 0 and returns false. Otherwise it adds `dt` to `Time`, wraps when `Loop` is set, and clamps when it is not. A non-finite `Time` becomes 0. If the new `Time` is less than `LastTime`, it clears `KeyCursors`, then stores the new `Time` into `LastTime`. That clear runs even when the entity is hidden and `SampleLocals` will not run this frame.

`SampleLocals` writes `playback.Locals` in place. Each channel starts at its cursor and moves forward. Step selects the latest key at or before `Time`. Linear lerps translation and scale and uses `Quaternion.Slerp` for rotation. Before the first key, use the first key. After the last, use the last. A missing channel keeps the joint's rest value. Local matrix:

```csharp
Matrix4x4.CreateScale(scale)
    * Matrix4x4.CreateFromQuaternion(rotation)
    * Matrix4x4.CreateTranslation(translation);
```

`BuildPalette` walks roots (`Parent < 0`) with `global = local`, and every child with `global = local * parentGlobal`. Write `joints[i].InverseBind * global` into the palette. `BuildRest` is the same walk with rest locals and no clock.

`SkinnedPoseSystem` in `Engine/Scene/Systems` has `Priority => 145` and calls `SkinnedPose.Advance`. Register it in `SceneSystemsFactory.PopulateSystemManager` next to `SceneRenderSystem`. The factory already receives `IModelFactory`. No new singleton.

In `EditorViewport.RenderSceneToFramebuffer`, the edit branch calls `SkinnedPose.Advance(scene.Context, modelFactory, deltaTime)` after `UpdateWorldTransforms` and before `SceneRenderPipeline.RenderScene`. The play branch stays `OnUpdateRuntime` only.

## 6. Uniform buffer

Why: OpenGL 3.3 guarantees 1024 vertex uniform components. 128 matrices do not fit there. A uniform block of 8192 bytes fits the 16KB minimum. This is the first uniform buffer in the engine, so the GL calls stay in the platform project.

```csharp
namespace Engine.Renderer.Buffers;

public interface IUniformBuffer : IDisposable
{
    void Update(ReadOnlySpan<float> std140);
    void BindBase(int bindingPoint);
}
```

`OpenGLUniformBuffer` creates one 8192-byte `GL_UNIFORM_BUFFER` with `GL_DYNAMIC_DRAW` on the first `BindBase`, uploads with `BufferSubData`, and binds with `BindBufferBase`. Register it as `IUniformBuffer` in `EngineIoCContainer`, singleton. `Graphics3D` takes that `IUniformBuffer`. It does not construct a second buffer.

`Update` receives matrices already transposed. `SkinnedPose.WriteBlock` writes `Matrix4x4.Transpose(palette[i])` into a reused `float[]` of length `jointCount * 16`. That is the same storage as instanced model matrices: the shader multiplies `vec4 * mat4` and translation stays in the last row. Reuse the float array on `Graphics3D`. Do not allocate it per draw.

## 7. Shaders

Add `ShaderId.SkinnedModel`, `SkinnedDepth`, `SkinnedPointDepth`, `SkinnedViewNormalModel`, and `SkinnedEmissive`.

| Program | Vertex | Fragment |
|---------|--------|----------|
| SkinnedModel | `skinnedModel.vert` | existing `modelShader.frag` |
| SkinnedEmissive | `skinnedModel.vert` | existing `emissive.frag` |
| SkinnedDepth | `skinnedDepth.vert` | existing `depth.frag` |
| SkinnedPointDepth | `skinnedPointDepth.vert` | existing `pointDepth.frag` |
| SkinnedViewNormalModel | `skinnedViewNormalModel.vert` | existing `viewNormal.frag` |

`#version 330 core`. The block and the skin are the same in each vertex shader:

```glsl
layout(std140) uniform JointPalette
{
    mat4 u_Joints[128];
};

layout(location = 6) in ivec4 a_Joints;
layout(location = 7) in vec4 a_Weights;

mat4 SkinMatrix()
{
    float sum = a_Weights.x + a_Weights.y + a_Weights.z + a_Weights.w;
    if (sum <= 0.0)
        return mat4(1.0);
    return u_Joints[a_Joints.x] * a_Weights.x
         + u_Joints[a_Joints.y] * a_Weights.y
         + u_Joints[a_Joints.z] * a_Weights.z
         + u_Joints[a_Joints.w] * a_Weights.w;
}
```

Apply it before the entity matrix, row-vector, matching `modelShader.vert`:

```glsl
mat4 skin = SkinMatrix();
vec4 skinned = vec4(a_Position, 1.0) * skin;
vec4 worldPos = skinned * model;
vec3 skinnedNormal = a_Normal * mat3(skin);
vec3 skinnedTangent = a_Tangent * mat3(skin);
```

The color shader then runs its existing normal-matrix and bitangent cross on `skinnedNormal` and `skinnedTangent`. Depth shaders skin position only and still pass the texture coordinate through for cutout. A joint with non-uniform scale can tilt a normal. That matches the LearnOpenGL skeletal article, which also uses the upper 3×3.

Wire the paths in `EngineShaderPaths.Resolve`.

## 8. Draw

Add `DrawSkinnedMesh` on `IGraphics3D`:

```csharp
void DrawSkinnedMesh(Mesh mesh, Matrix4x4 entityWorld, ReadOnlySpan<Matrix4x4> palette,
    Vector4 tint, int entityId, float metallic, float roughness, float ao, Vector3 emissive);
```

Why: a skinned mesh cannot join `DrawMeshInstances`. Each entity has its own palette.

The method mirrors the single-instance path inside `DrawMeshInstances`. It picks the skinned program for the active pass (color, view-normal, directional depth, point depth), writes the transposed block, binds binding point 0, sets `u_Model` to `entityWorld`, and draws indexed. The color pass then does the emissive add with `SkinnedEmissive` when that mesh has an emissive map. It sets `u_Instanced` to 0 on programs that still declare it. It does not build an instance buffer.

In `SceneRenderPipeline`, both submesh loops (`SingleSubmesh` and the all-submesh walk) branch before `QueueSubmesh`:

```csharp
if (model!.Skin is null)
{
    QueueSubmesh(modelRenderer, transform, tint, entity.Id, submesh, ref stats);
    continue;
}

var world = transformComponent.GetWorldTransform();
if (ShouldSkipOpaqueDraw(hasFrustum, frustum, world, bounds, ...))
    continue;
graphics3D.DrawSkinnedMesh(
    submesh, world, RestOrPlayback(entity, model), tint, entity.Id, ...);
```

`RestOrPlayback` uses the component palette when `PoseReady` is true and `Pose.Length` equals `model.Skin.Joints.Length`. Otherwise it uses `model.RestPose`. `transform` still includes the pivot, and this branch does not use it when the model has a skin. Every submesh of that model takes this path, including one whose weights are zero.

The cull box is `submesh.Bounds` in that draw matrix. Do not grow it to the clip.

`modelRenderer.Pivot` is not applied when `model.Skin` is set.

## 9. Editor spawn

Why: unpacking would skin a mesh that is also parented at the bind pose.

In `ImportModelHierarchyCommand.Execute`, when `model.Skin` is set:

- Leave `SuppressDraw` false and `MeshIndex` null.
- Do not bake the first mesh node into the root transform.
- Set `ModelRendererComponent.Pivot` to zero.
- Parent lights with `SpawnPackedLights`'s walk using an identity inverse, so a lamp's local matrix is its transform from the file root.
- If the root has no `SkinnedAnimationComponent`, add one. If it has one, leave `ClipName` and `Loop` alone.

`SkinnedAnimationComponentEditor` is a `ComponentEditor<SkinnedAnimationComponent>` registered with `RegisterMany` in `EditorIoCContainer`. It loads the model through the existing `EditorModelLoadService`, draws a combo of an empty row plus `Skin.Clips` names, and a loop checkbox through `UIPropertyRenderer`. Writes go through `IEditorHistory` the way `ModelRendererComponentEditor` writes fields. The runtime project does not reference this type.

## 10. Tests

`RestPose_TwoJointChain_IsIdentity` in `Engine.Tests`: a child joint parented to a root, inverse bind equal to the inverse of the rest global, palette identity within 1e-4. That fails if the multiply order regresses. Build the chain in the test. Do not load a golden file.
