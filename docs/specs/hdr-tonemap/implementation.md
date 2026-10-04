# HDR Framebuffer and Tonemap — Implementation

Step-by-step code for the design in `introduction.md` and `developer-guide.md`. Paste the C# into the existing types. The GLSL is the shader half of the same change.

There is no new component, no new system, and no scene field. `TonemapPass` is a singleton next to `FxaaPass`. `SceneRenderPipeline` and `Graphics3D` do not gain this draw.

## 1. Scene color format

`FrameBufferFactory.Create()` uses `RGBA16F` for attachment 0. The entity-id and depth attachments stay.

`FxaaPass.Target` uses `RGBA16F` for the scene target (`depth: true`). The FXAA resolve target stays `RGBA8`.

Both color specs keep linear filtering and clamp to edge.

**Why:** The viewport and the player scene target are the two buffers the world is drawn into. The resolve target is the picture FXAA already knows how to sample.

## 2. Shaders that stop encoding

In `skybox.frag`, `cube.frag`, and `modelShader.frag`, delete `Encode` and write the linear sum. Alpha stays the value those shaders already output.

```glsl
o_Color = vec4(ambient + sun + lamps, u_Color.a);
```

The sky writes the sampled environment with alpha 1 and entity id −1. No Reinhard and no gamma in that fragment.

`textureShader.frag` and `lineShader.frag` stay as they are.

Add `ShaderId.Tonemap`. `EngineShaderPaths` maps it the way `SelectionOutline` reuses `fxaa.vert`:

```csharp
if (shader == ShaderId.Tonemap)
    return (Path.Combine(dir, "fxaa.vert"), Path.Combine(dir, "tonemap.frag"));
```

`tonemap.frag`:

```glsl
#version 330 core

layout(location = 0) out vec4 o_Color;

in vec2 v_TexCoord;

uniform sampler2D u_Color;

void main()
{
    vec4 sampleColor = texture(u_Color, v_TexCoord);
    vec3 rgb = sampleColor.rgb / (sampleColor.rgb + vec3(1.0));
    rgb = pow(rgb, vec3(1.0 / 2.2));
    o_Color = vec4(rgb, sampleColor.a);
}
```

**Why:** `fxaa.vert` already covers the viewport with three vertices and writes `v_TexCoord`. A second vertex shader would repeat that. Alpha is the sprite and tint alpha. Compressing it would fade quads that were opaque.

## 3. TonemapPass

Register it beside `FxaaPass`:

```csharp
container.Register<TonemapPass>(Reuse.Singleton);
```

The constructor takes `IRendererAPI`, `IShaderFactory`, `IVertexArrayFactory`, and `IFrameBufferFactory`. Init creates the program and `vertexArrayFactory.Create()`, sets `u_Color` to 0, and builds an `RGBA8` target with linear filter and clamp, no depth. A failure logs once and leaves `Available` false.

```csharp
public IFrameBuffer? Resolve(IFrameBuffer scene)
{
    var spec = scene.GetSpecification();
    if (!Available || spec.Width == 0 || spec.Height == 0 || !Ensure(_output, spec.Width, spec.Height))
        return _output;

    Draw(scene.GetColorAttachmentRendererId(), spec.Width, spec.Height, _output);
    return _output;
}
```

`Draw` is public and matches `FxaaPass.Apply`: bind `dest` or the default framebuffer, set the viewport, turn depth test, blend, and face culling off, bind the source on unit 0, `DrawArrays(triangle, 3)`, then restore those three states and unbind `dest`. `dest.Bind` and `dest.Unbind` already save and restore the previous framebuffer and viewport.

`Ensure` resizes when the size changes. If create or resize throws, log once, dispose that target, and return false. The next call may create it again.

**Why:** `Resolve` is what the viewport needs, because ImGui, FXAA, and the outline want a framebuffer. Before the first successful draw `_output` is null, so the caller skips the image and the frame stays empty. After that, a failed draw returns the existing display target and does not sample the float attachment. When the player has no FXAA, `Draw` reads the scene color and writes the window. That is the only encode on that path.

## 4. Editor

In `EditorViewport.LayoutAndRender`, after `RenderSceneToFramebuffer`:

```csharp
var ldr = tonemapPass.Resolve(_frameBuffer);
if (ldr == null)
    return;

var display = editorPreferences.Fxaa ? fxaaPass.Resolve(ldr) : ldr;
if (selected.Count > 0 && sceneContext.State == SceneState.Edit)
    display = selectionOutlinePass.Resolve(display, _frameBuffer, ids);
```

The viewport takes `TonemapPass` in its constructor. Returning before `ImGui.Image` leaves the widget empty on the first failed frame and leaves the previous image up when ImGui does not get a new one. Do not pass `_frameBuffer` to `ImGui.Image` or to `fxaaPass.Resolve`.

**Why:** `_frameBuffer` is the float scene. `Resolve` on it would make FXAA sample linear light. The outline still receives `_frameBuffer` as its entity-id source. Its color source is `display`.

## 5. Player

`FxaaPass.Present` keeps drawing the callback into `_scene`, which is now `RGBA16F` plus depth. It stops sampling `_scene` directly for FXAA, and it stops calling `draw()` on the default framebuffer when the scene target cannot be created.

```csharp
public IFrameBuffer? DrawScene(uint width, uint height, Action draw)
{
    if (width == 0 || height == 0 || !Ensure(ref _scene, width, height, depth: true))
        return null;

    _scene!.Bind();
    try
    {
        draw();
    }
    finally
    {
        _scene.Unbind();
    }

    return _scene;
}
```

`GameLayer.OnUpdate` takes `TonemapPass` and becomes:

```csharp
var hdr = fxaaPass.DrawScene((uint)(size.X * scale), (uint)(size.Y * scale), () =>
{
    graphics3D.SetSkybox(scene.Skybox);
    graphics2D.SetClearColor(scene.BackgroundColor);
    graphics2D.Clear();
    scene.OnUpdateRuntime(timeSpan);
});
if (hdr == null)
    return;

if (fxaaPass.Available)
{
    var ldr = tonemapPass.Resolve(hdr);
    if (ldr == null)
        return;

    fxaaPass.Apply(ldr.GetColorAttachmentRendererId(), ldr.GetSpecification().Width, ldr.GetSpecification().Height, dest: null);
}
else
    tonemapPass.Draw(hdr.GetColorAttachmentRendererId(), hdr.GetSpecification().Width, hdr.GetSpecification().Height, dest: null);
```

**Why:** The player has no viewport framebuffer of its own. `DrawScene` is the existing `Present` split at the point where the float image exists and the display image does not. Skipping the frame leaves the window on its previous contents.

## 6. Tests

`FrameBufferFactory.Create()` is asserted to use `RGBA16F` for color, `RED_INTEGER` for the id, and depth. No window.

`TonemapPass` with a fake renderer: a 0 size does not draw, and the result is null when nothing has been drawn yet. A second `Resolve` after success does not call `IShaderFactory.Create` again. A factory that throws on `Create` leaves `Available` false, and `Resolve` does not draw.

The viewport wiring is covered by the call order above: FXAA off feeds ImGui from `Resolve`, FXAA on passes that framebuffer to `fxaaPass.Resolve`, and the outline receives the scene framebuffer as the id source.

Where a GL context exists, clear the scene target to linear white, `Resolve`, and read the center of the display image. The max channel is near 186, not 255. That read is not a gate on macOS.

**Why:** 186 is `round(255 * pow(0.5, 1/2.2))`. A white clear that survives unchanged would mean the pass was skipped or the scene attachment was still 8-bit.
