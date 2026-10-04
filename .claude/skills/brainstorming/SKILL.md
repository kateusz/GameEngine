---
name: brainstorming
description: "Designs features that fit this engine's ECS, editor, and runtime before any source changes. Use for a new subsystem, render path, physics, serialization, scripting API, or a behavior change with a real trade-off. Reads docs/architecture first, then writes docs/specs/{name}/introduction.md, developer-guide.md, and implementation.md. Do not use for bug fixes, renames, DI registration, or adding a field to an existing pattern. Games under games/ or assets/scripts/ use game-creation. After the docs are approved, components and systems are implemented with component-workflow or system-creation."
---

# Brainstorming Ideas Into Designs

Turn an idea into a design that fits this engine. The only output is three documents in `docs/specs/{name_of_feature}/`. Writing engine source is out of scope.

<HARD-GATE>
Do NOT invoke `component-workflow`, `system-creation`, `game-creation`, writing-plans, or any other implementation skill. Do NOT create or edit source files, tests, or scenes. This skill stops when `introduction.md`, `developer-guide.md`, and `implementation.md` are written and the user has reviewed them.
</HARD-GATE>

## When to use

Use this skill when the work is a new subsystem, a new render path, physics, serialization, a scripting API, or a behavior change with a real trade-off. Getting the design wrong would waste an implementation pass.

**Do not use it** for a bug fix, a rename, a config tweak, DI registration, a menu entry, or adding a field by copying an existing pattern. Do that work directly.

**Hand off:**

- A game under `games/` or `assets/scripts/` uses `game-creation`.
- After the user approves these docs, a new component uses `component-workflow` and a new system uses `system-creation`.
- This skill ends at `docs/specs/{name_of_feature}/`.

## Checklist

Create a task for each item and finish them in order. Stop at item 6.

1. **Read the engine** — `docs/architecture/` and the nearest existing subsystem, then classify the idea
2. **Ask the engine questions** — one unanswered question per message, in the order below
3. **Propose 2–3 engine approaches** — recommended variant with rationale, pros/cons per variant, then the simplicity filter
4. **Present the design** — in sections, approval after each section
5. **Write the three docs** — `introduction.md`, `developer-guide.md`, `implementation.md`
6. **Self-review and ask the user to review** — then stop

## 1. Read the engine first

Before the first question, read [docs/architecture/README.md](../../../docs/architecture/README.md) and the architecture doc for the subsystem you are about to touch.

| Topic | Doc |
|-------|-----|
| ECS | [ecs-architecture.md](../../../docs/architecture/ecs-architecture.md) |
| Frame, editor vs runtime | [game-loop.md](../../../docs/architecture/game-loop.md) |
| Render | [scene-rendering-pipeline.md](../../../docs/architecture/scene-rendering-pipeline.md) |
| Lighting / shadows | [lighting.md](../../../docs/architecture/lighting.md), [shadows.md](../../../docs/architecture/shadows.md) |
| Scripts | [scripting-lifecycle.md](../../../docs/architecture/scripting-lifecycle.md) |
| Physics | [physics-system.md](../../../docs/architecture/physics-system.md) |
| Audio | [audio-system.md](../../../docs/architecture/audio-system.md) |
| Scenes | [serialization.md](../../../docs/architecture/serialization.md) |
| DI | [dependency-injection.md](../../../docs/architecture/dependency-injection.md) |

Also read the closest existing code for that subsystem. Proposals extend what is already here:

- Pure ECS (`Entity`, data-only `IComponent`, priority-ordered `ISystem`)
- Engine core separate from the ImGui editor and from the Runtime player (no editor in a published game)
- OpenGL only behind the renderer abstraction (OpenGL 3.3+)
- Box2D for 2D physics
- DryIoc constructor injection, no static singletons
- Roslyn hot-reload scripting

**Classify on three axes before asking anything:**

1. **2D, 3D, or both.** Today the 2D path is a sprite batch with the depth test off. The 3D path is forward shading of static meshes with the depth test on. One `SceneView` per `RenderScene`. 2D and 3D do not share depth. Sprites run to completion, then the 3D sequence runs.
2. **Who uses it.** A built-in engine component, a game scripting API, or an editor-only tool.
3. **Where the data lives.** A component on an entity, an asset, system state, or a serialized scene.

If the idea needs something the engine does not have, say so immediately and split the work. Examples: 3D physics, a parent/child hierarchy, a deferred pipeline, a separate 2D camera and 3D camera inside one `RenderScene`. State what fits now and what the missing engine capability is. Brainstorm only the piece that fits, unless the user chooses to design the missing capability first.

If the request is several independent subsystems, split them before detail questions. Each piece gets its own `docs/specs/{name}/`. Brainstorm the first piece only.

## 2. Questions

One question per message. Prefer multiple choice. Ask the next question in this list that is not already settled by the request or the classification:

1. What problem this solves, and what success looks like
2. 2D, 3D, or both — and whether the current depth policy and pass order may change
3. Hot frame path, or editor time
4. Whether the state must be saved in the scene and survive load
5. Whether Runtime without ImGui must do this
6. Whether this touches several independent subsystems — if yes, split, then brainstorm the first piece

If a question loop stops making progress, ask the user what to lock and continue.

## 3. Approaches

Propose 2–3 approaches that are real choices in this architecture. Typical forks:

- Component + system, or a scripting API
- Extend `SceneRenderPipeline`, or add a pass
- Data on a component, or an asset
- One path shared by 2D and 3D, or a separate path

Present them in one message using this structure:

### Recommended: [name of approach]

State the recommendation up front. Explain **why** this option wins for this feature (fit with ECS, editor/runtime split, serialization, frame cost, DryIoc, renderer abstraction, and the classification from step 1). Two to four sentences; tie the reasoning to constraints the user already accepted.

### Variant comparison

For **every** approach (including the recommended one), use the same subsections:

**[Approach name]** (mark with **Recommended** on the chosen one)

- **Pros** — what this option does well (bullets)
- **Cons** — costs, risks, or mismatches with this engine (bullets)

In pros and cons, cover where relevant: frame cost, editor/runtime split, serialization, DryIoc fit, and whether OpenGL stays behind the renderer abstraction.

Apply [Performance](#performance) and [OpenGL standards](#opengl-standards) while comparing. A cheap performance win belongs in the recommendation. A rendering approach that disagrees with established OpenGL practice is not a valid option until that disagreement is explicit.

**Simplicity filter**, after the comparison: say whether the recommended approach can be a smaller change inside an existing system. If it can, name that smaller change, update the recommendation if needed, and briefly say why the simpler fork still meets the goal.

## 4. Present the design

Present the design in sections scaled to their complexity (a few sentences when straightforward, up to 200–300 words when not). After each section, ask whether it looks right.

Cover architecture, components, data flow, error handling, testing, the editor/runtime split, the cheap performance win, and — when rendering is involved — the OpenGL-practice fit.

Stay inside the goal. Note a local problem in code you had to touch (a file that is already too large, a boundary that is already wrong). Do not propose unrelated refactoring.

## Performance

Look for performance while designing. Do not run an aggressive optimization pass and do not add complexity, subsystems, or speculative scale work for speed.

When a **cheap win** exists, propose it and say why it is cheap. A cheap win is a small change on the design already chosen: fewer draw calls, less GL state thrashing, no per-frame allocation, no GPU readback, reuse of an existing batch or instancing path, data laid out so the hot loop stays tight.

Record the proposal in the design even if it is optional. If the only faster version costs a new abstraction or a pipeline rewrite, name that cost and leave it out of the recommendation.

## OpenGL standards

Any design that touches rendering, shaders, meshes, textures, framebuffers, or GL state must follow established OpenGL practice.

Before locking that part of the design, check it against [LearnOpenGL](https://learnopengl.com/) and the same practices in other solid OpenGL references (core-profile material for OpenGL 3.3+). Confirm the design matches what those sources show for the topic: buffer and vertex-array setup, shader pipeline, textures and units, framebuffer usage, coordinate spaces, and state changes.

Do not invent GL usage, object lifetimes, or pipeline steps those references do not support. If the engine already diverges, or the design must diverge, say so explicitly and justify it against the reference. `implementation.md` must stay consistent with that check. Engine core still talks to the renderer abstraction, not to raw GL calls scattered outside `Platform/`.

## 5. Documents

After the user approves the design, write three files to `docs/specs/{name_of_feature}/`. No repeated prose across files.

### `introduction.md`

Problem, terminology, and how the feature works. No code and no pseudocode.

### `developer-guide.md`

Steps and why each step matters, a short implementation glossary (not a copy of the introduction), and Mermaid diagrams. Pseudocode only where the logic is non-obvious. No C#.

### `implementation.md`

A C# sketch for this engine, step by step, with a brief why. Cover only what the feature needs:

- Component
- System and its priority
- DryIoc registration
- Serialization
- Where it sits in the pipeline, in the editor, and in Runtime
- Tests worth adding

This file is part of the design. Do not create the source files it describes.

## 6. Self-review, then stop

Fix these inline. Do not re-review after the fix.

1. **Placeholders.** No "TBD", "TODO", or a requirement that can be read two ways.
2. **One feature.** One directory. If the docs describe several independent subsystems, split them.
3. **Three files agree.** The guide matches the introduction. The C# sketch matches the guide. No paragraph is copied from one file into another.
4. **Code in the right file.** `introduction.md` has no code. `developer-guide.md` has no C#. Only `implementation.md` has C# sketches.
5. **Engine checklist.** Editor work stays out of Runtime. OpenGL stays behind the renderer abstraction. 2D physics stays on Box2D. The 2D/3D depth policy and pass order are unchanged, or the change is explicit. Scene state that must survive save/load has a serialization step. Hot paths do not allocate per frame.
6. **Performance.** A cheap win is proposed. A performance idea that became a new subsystem is out of the recommendation.
7. **OpenGL.** If the feature touches GL, the design matches [LearnOpenGL](https://learnopengl.com/) and equivalent references, or the divergence is named and justified.

Then ask:

> "Please review them and let me know if you want to make any changes."

Wait. If the user wants changes, edit the docs and run this self-review again. When the user approves, stop. Do not implement.

## Key principles

- **One question at a time.** Multiple choice when you can.
- **Fit this engine.** Extend ECS, the editor/runtime split, the renderer abstraction, Box2D, DryIoc, and Roslyn scripting.
- **Say what the engine lacks** as soon as you see it, and split the work.
- **Two or three architectural forks** — recommended option with why, explicit pros and cons per variant, then the smaller-change filter.
- **Approve section by section** before writing docs.
- **Cheap performance, not a perf project.** Propose a gain when it is cheap.
- **OpenGL the way the references show it.** Match [LearnOpenGL](https://learnopengl.com/). Name any divergence.
- **Three docs, then stop.** No source files.
