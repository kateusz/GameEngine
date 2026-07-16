# Physics Raycasting & Shape Queries — Introduction

## Problem

The physics system today answers only one kind of question: “what just collided?” Contact begin/end callbacks fire during the simulation step when bodies touch. That is enough for bounce, scoring, and trigger zones that react to presence.

Gameplay constantly needs a different question: “what is in this direction or region *right now*?” Ground checks, line-of-sight, aim/shoot hits, and proximity probes are spatial **queries**. They are not events. They are synchronous reads of the physics world’s current shape layout. Without them, scripts either fake answers with contact flags or invent parallel geometry checks that drift from the real colliders.

## What this feature delivers

- **Closest-hit raycast** — cast a segment through the world and learn the nearest solid (or optionally trigger) collider along that path.
- **Single-hit circle overlap** — ask whether a circle in world space overlaps a collider, and if so return one hit.
- **Ignore-self filtering** — a query can skip one entity (typically the caller) so characters do not “see” their own feet or hurtbox.
- **Optional trigger inclusion** — by default queries hit solid colliders only; a per-call flag includes trigger fixtures.
- **Script-friendly helpers** — entity scripts call thin wrappers that forward to the scene physics world with ignore-self filled in.
- **World-owned API** — the real query methods live on the physics world abstraction so systems can query without going through an entity.

## What this feature explicitly does not do (v1)

- **All-hits / piercing rays** — only the closest hit is returned.
- **Point tests and box/AABB overlaps** — circle overlap is the only shape query.
- **Physics layers / category masks** — no collision matrix filtering yet; ignore-entity is the only filter beyond solid vs trigger.
- **Editor mouse picking** — viewport picking via physics is out of scope.
- **New collider shapes** — queries hit whatever fixtures already exist (today: boxes). Circle colliders remain a separate feature.
- **Contact-callback changes** — begin/end collision and trigger events stay as they are.

## Key terminology

**Spatial query.** A synchronous read of the physics world’s current geometry. It does not move bodies and does not enqueue contact events.

**Raycast.** A query along a directed segment from an origin in a direction up to a maximum distance. Returns the closest qualifying hit, or no hit.

**Raycast hit.** The result of a successful raycast: which entity was hit, where, surface normal, distance along the ray, and whether the fixture was a trigger.

**Overlap circle.** A query that tests a circle (center + radius) against world colliders and returns one qualifying hit, or no hit. Which overlapping collider is chosen is unspecified in v1 when several qualify.

**Overlap hit.** Same result shape as a raycast hit where fields make sense (entity, point, distance from center, trigger flag). Normal may be unavailable or zero when the backend does not provide one cheaply.

**Ignore entity.** An optional entity whose fixtures are skipped for that query. Script helpers pass the calling entity.

**Include triggers.** Per-call option. Default false: trigger fixtures are invisible to the query. When true, triggers can be returned as hits and are marked as such on the result.

**Solid collider.** A fixture that participates in physical collision response (`IsTrigger` false).

**Trigger collider.** A fixture that detects overlap without physical blocking (`IsTrigger` true).

**Physics world.** The per-scene simulation container behind the engine’s 2D physics abstraction. Bodies and fixtures live here; queries run against this live state.

**Body–entity mapping.** Each simulated body is associated with an ECS entity. Query hits resolve to entities through that mapping — the same path contact events already use.

## Patterns and principles

**Queries are orthogonal to contacts.** Contacts are push notifications during `Step`. Queries are pull reads at any time the world exists. Mixing the two models (e.g. faking ground checks with sticky contact flags) is what this feature replaces.

**One abstraction, one backend wrap.** Game and engine code call the physics world interface. The Box2D backend implements queries by wrapping native raycast and circle query facilities. No parallel spatial index in the engine core.

**Fail soft for gameplay.** Missing the world, missing a hit, or invalid numeric inputs should yield “no hit,” not throw through script update paths. Defensive skips apply when a fixture has no entity mapping.

**Minimal filter surface.** v1 only needs ignore-self and solid-vs-trigger. Layer masks belong with a future collision matrix; inventing a half-mask API now would create throwaway contracts.

**Script convenience without hiding the world.** Helpers on scriptable entities exist so common gameplay is one line. The world remains the source of truth so non-entity systems are first-class callers.

## Architecture philosophy

**Extend the existing world, don’t invent a sibling service.** Two methods do not justify a separate queries interface. Keeping queries on the physics world matches “the world owns simulation state.”

**Reuse mapping and fixtures.** Hits are only as good as the bodies already created for rigidbody + collider entities. Queries do not create geometry; they read what simulation already registered.

**Ship the M1 mechanic unlock, defer the suite.** Closest ray + one circle hit unlocks ground checks, shooting, and LOS. All-hits, point tests, AABB, editor pick, and layers are real needs — each is a follow-up with clear demand signals, not prerequisites for the first useful API.

**Lazy senior defaults.** Closest-only raycast, any-one overlap hit, triggers off by default, ignore-self on script helpers. Document the overlap non-ordering so callers do not assume “closest overlapping” until that is explicitly added.
