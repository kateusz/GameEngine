# Selection Outline — Conceptual Introduction

## What Problem Does This Solve?

The editor can select one entity, and the hierarchy marks that row, but the viewport picture does not. A mesh, a sprite, or a model sitting among others looks the same whether it is selected or not. The person editing has to trust the hierarchy, or click again and watch the hover name.

This feature draws a one-pixel orange edge around the visible pixels of the selected entity. The edge is on the picture the viewport already shows, in Edit and in Play.

## What the Feature Will Achieve

- The selected entity gains an orange silhouette edge, one framebuffer pixel wide, in the four directions up, down, left, and right.
- Pixels that belong to that entity keep their own color. The edge sits just outside them.
- The same edge appears in Edit and in Play, inside the editor viewport. The standalone game has no selection, so it never draws the edge.
- Only that one entity is outlined. Children, parents, and the rest of the scene are not.
- An entity that drew no pixels (a light, a camera, an empty node) gets no edge. The picture stays as it was.
- Picking still reads the entity-id image from the scene. The outline does not replace that image.
- If the outline cannot be created, the viewport shows the same picture it shows today.

## Benefits and Outcomes

| Outcome | Why it matters |
|---------|----------------|
| Visible selection | The chosen object is marked in the picture, not only in the hierarchy |
| Same in Play | A selection made before Play stays visible while the game camera runs |
| Unchanged pixels | The entity itself is not tinted, so materials stay readable |
| Safe failure | A missing shader or buffer turns the edge off and leaves the frame alone |
| No new authoring | Nothing is added to the inspector. The existing selection is the input |

## Terminology

**Entity-id image** — The integer picture the scene already draws beside its color. Each pixel stores the id of the entity that covered it, or a non-positive value where nothing did. Picking reads one pixel of this image. The outline reads the whole image.

**Silhouette edge** — The screen pixels that are not the selected id, but touch a pixel that is, one step up, down, left, or right. Those pixels become orange. Diagonal neighbors do not.

**Displayed color** — The color image about to be shown in the viewport. That is the scene color, or the FXAA result when FXAA is on. The outline reads this image and writes a separate color image. It never writes the scene image.

**Selected id** — The id of the one selected entity. Zero and negative values are not a selection. They are what empty pixels already store.

## Pattern

The edge is a full-screen comparison, not a second draw of the mesh. The scene has already decided which entity owns each pixel, including occlusion, sprites, and models. The outline only asks, for each pixel, whether that record says the selected entity is here or next door.

That is why a hidden surface gets no edge, and why a sprite and a mesh use the same pass. The pass does not know what an entity is. It knows one integer and two images.

## Architecture Philosophy

The outline is the last picture step in the editor viewport, after the scene and after FXAA. Three responsibilities stay apart:

1. **Scene** — writes color and entity ids, as it already does
2. **Outline pass** — reads the displayed color and the entity-id image, writes one color image, knows one id
3. **Viewport** — decides to run the pass when an entity is selected, in Edit and in Play, and shows the result

The pass does not know about hierarchy, play mode, or ImGui. The game runtime does not know about the pass. Gizmos drawn by ImGui after the image stay on top of the edge. Overlay lines that store a non-positive id, such as the grid, are not part of the selection.

## Out of Scope

- Outlining children or a whole subtree
- A thicker edge, a different color, or a setting for either
- An edge in the standalone game
- An edge for an entity that did not draw pixels
- A preview of the shadow map
- Writing into the scene image or the entity-id image
