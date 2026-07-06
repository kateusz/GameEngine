# Output Format

## Per-issue template

For every issue:

### Severity

Critical / High / Medium / Low

### Location

Class, interface, namespace, module, or subsystem.

### Problem

Describe the architectural problem.

### Why it matters

Explain the root cause.

### Long-term consequences

How this affects future development.

### Recommended redesign

Architectural solution only — no implementation detail. Must already reflect the ponytail challenge: no speculative abstractions, no fix bigger than the problem warrants.

### Ponytail challenge

Mandatory. Summarize how ponytail rules were applied to this finding and redesign. Include tags (`delete:`, `yagni:`, `shrink:`, etc.) where relevant. State whether the issue survived, was revised, downgraded, or dropped — and why.

### Expected benefits

What improves after redesign.

---

## Final summary

Provide all ratings (1–10 per [rating scales](SKILL.md#rating-scales)):

- Overall Architecture
- Scalability
- Maintainability
- ECS Compatibility
- Editor/Runtime Separation
- Runtime Decoupling (coupling dimension)
- Public API
- Extensibility

### Top Five Architectural Improvements

Rank the five changes with the greatest long-term architectural benefit.

---

## Worked example

### Severity

High

### Location

`SceneManager` — loads assets and pushes render commands during scene switch.

### Problem

Scene orchestration directly calls `TextureLoader` and `RenderQueue`, coupling scene lifecycle to asset I/O and the renderer.

### Why it matters

Scene switching owns three concerns (lifecycle, assets, rendering). Changes to load policy or render batching force scene code edits.

### Long-term consequences

Additive scenes and streaming need async load orchestration; current design will sprawl conditionals or duplicate load paths. Editor play-mode and standalone runtime diverge.

### Recommended redesign

Scene switch publishes a `SceneLoadRequested` event. Existing `AssetSystem` and `RenderSystem` subscribe and handle their domains. `SceneManager` tracks load state only.

### Ponytail challenge

Initial proposal added `ISceneLoadOrchestrator` + `ISceneLoadPhase` enum. `yagni:` one implementation — dropped interface, kept event on existing bus. `shrink:` two subscribers, no new types. **Revised — survived at High.**

### Expected benefits

Scene code shrinks; asset and render policies evolve independently; streaming adds a subscriber without touching `SceneManager`.
