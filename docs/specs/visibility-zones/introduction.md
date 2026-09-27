# Visibility Zones — Conceptual Introduction

## What Problem Does This Solve?

Frustum culling only asks whether geometry intersects the camera or light pyramid. In a dense interior such as a castle, hundreds of meshes can sit inside that pyramid while hidden behind walls. The GPU still receives a draw call and material setup for each one. Depth testing hides pixels; it does not remove work from the CPU walk or from driver state changes.

Visibility zones add a coarse, author-controlled partition of the world. Only meshes assigned to zones that currently contain the camera are submitted, in both the color pass and the directional shadow pass. Meshes with no zone assignment keep today’s behavior: frustum and shadow caster distance only.

## What the Feature Will Achieve (v1)

- Authors place zone volumes as ordinary entities with an axis-aligned box in local space, transformed by the entity’s world matrix.
- Authors assign a zone to a model renderer by storing the zone entity’s id. A sentinel value means “do not use zones for this object.”
- Each frame, every zone whose world box contains the camera position contributes its entity id to an active set. If the camera lies in several overlapping zones, the active set is the union of all of them.
- A model renderer with a zone assignment is drawn only when its zone entity id is in that active set. The same rule applies in the directional shadow pass as in the color pass.
- If the camera is not inside any zone box, zone-assigned meshes are not drawn. Meshes without a zone assignment are still tested only by frustum culling and the existing shadow caster distance limit.
- Outdoor areas are modeled as one large zone (for example an entity named World) plus smaller interior zones. There is no special “outdoor flag”; behavior follows from box placement and assignment.
- Portals and automatic visibility through doorways are not part of v1. Adjacent rooms are visible only if the camera’s position falls inside overlapping zone boxes or multiple zone boxes at once.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Fewer color draws indoors | Corridor views no longer submit the whole courtyard behind the wall |
| Fewer shadow draws | The same filter runs in the shadow pass, aligning with directional shadow cost |
| Predictable authoring | Artists see wireframe boxes in the editor and assign zones explicitly |
| Safe opt-in | Scenes with no zone components behave exactly as before |
| Conservative correctness | Unassigned renderers still draw when in frustum; zone-assigned renderers never draw outside the active set |

## Terminology

**Zone entity** — A scene entity that carries a visibility zone component and usually a transform. Its identity is the entity id, not a separate zone key.

**Local zone box** — Minimum and maximum corners of an axis-aligned box in the zone entity’s local space. The world-space test uses the same eight-corner transform pattern as mesh bounds for frustum culling.

**Zone assignment** — On a model renderer, a reference to a zone entity id. The sentinel means the renderer ignores zones entirely.

**Active zone set** — The list of zone entity ids whose world boxes contain the camera position for the current frame.

**Union overlap** — When the camera lies inside more than one zone box, every such zone is active. Renderers in any of those zones may draw.

**Outside all zones** — The active set is empty. Zone-assigned renderers are skipped. Renderers without an assignment continue with frustum culling only.

**Has visibility zones** — A scene-level flag set when at least one zone component exists after load or editor changes. When false, the render pipeline does not run zone logic.

## Principles

**Zones before frustum for assigned meshes.** Zone rejection is cheap compared to batching and draw setup. It runs only for renderers with a zone assignment, then the existing frustum and shadow caster tests apply.

**One rule for shadow and color.** Directional shadow uses the same active zone set and the same camera position as color, so shadow draw counts drop together with color draws.

**Entity id as the only key.** No parallel guid table or display names in the component. The hierarchy name is for humans; the runtime link is entity id.

**No portal graph in v1.** Visibility between rooms is either overlap of boxes or future work. Document setup so castle interiors use intentional overlap at doorways or accept that only the current room’s assigned meshes draw until portals exist.

**Failure is visible but narrow.** A renderer pointing at a missing zone entity is skipped at play time with a single warning. Full editor validation of every assignment is deferred.

## Design Approach

The feature is a CPU filter inserted in the opaque walk, alongside frustum culling. It does not change shaders, instancing, or sprites. The editor adds component inspectors, a zone picker on model renderers, and wireframe visualization of zone boxes in the edit viewport. A later version may add portals as edges in a graph between zone entities; v1 deliberately stops at volumes and assignments.
