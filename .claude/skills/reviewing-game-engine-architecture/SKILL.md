---
name: reviewing-game-engine-architecture
description: Performs deep architectural review of a single game engine subsystem — decoupling, ECS fit, editor/runtime separation, scalability, maintainability. Use when the user asks for architecture review, subsystem design review, module architecture audit, or invokes /reviewing-game-engine-architecture.
---

# Reviewing Game Engine Architecture

Principal game engine architect — not a code reviewer. Evaluate whether the subsystem is architecturally correct, scalable, maintainable, and extensible for long-term engine development (multiple games, years of evolution). Experience spans Unity, Godot, Unreal, Bevy, Stride, and custom engines.

## Contents

- [Scope](#scope)
- [Workflow](#workflow)
- [Evaluation criteria](criteria.md)
- [Ponytail challenge](#ponytail-challenge)
- [Output format](output-template.md)
- [Rating scales](#rating-scales)
- [Review principles](#review-principles)

## Scope

Review **only the subsystem the user provides**. Ignore unrelated engine parts unless they directly affect this subsystem.

**Out of scope:** formatting, naming, comments, code style, minor refactoring.

**In scope:** architectural quality only.

## Workflow

1. Confirm the subsystem boundary with the user if ambiguous.
2. Read [criteria.md](criteria.md) and evaluate each themed group against the code.
3. For each issue: draft finding → apply [ponytail challenge](#ponytail-challenge) → revise or drop → record using [output-template.md](output-template.md).
4. Produce the final summary with rated dimensions and top five improvements.

Do not batch ponytail challenges at the end. Apply per issue, in order.

## Ponytail challenge

For **every issue**, challenge your own **Recommended redesign** before including it in the report.

If the **ponytail-review** skill is available, read and follow it. Treat the redesign as the diff under review.

**Inline rules** (self-contained when ponytail-review is unavailable):

- Hunt over-engineering in **your proposed fix**, not just existing code.
- Tags: `delete:` (cut entirely), `stdlib:` (use existing library), `native:` (use platform feature), `yagni:` (speculative abstraction), `shrink:` (same logic, fewer lines).
- Ask: real architectural problem or premature abstraction hunting? Fix adds layers/interfaces with one caller? Smaller change (delete, inline, shrink) enough? YAGNI?
- Outcomes: revise, downgrade severity, drop the issue, or `Proposal lean. Keep.` / `Lean already. Ship.`

Record results under **Ponytail challenge** in each finding (see [output-template.md](output-template.md)).

## Rating scales

All ratings use **1–10** (1 = excellent, 10 = severe problem). Apply consistently:

| Dimension | 1 | 5 | 10 |
|-----------|---|---|-----|
| Overall architecture | Clean boundaries, proven patterns | Manageable debt | Fundamental redesign needed |
| Coupling | Fully inverted, testable in isolation | Some hidden/temporal coupling | Circular, hard-wired cross-cutting |
| Scalability | Handles target scale with headroom | Works today, known ceiling | Breaks at modest growth |
| Maintainability | Obvious contracts, low duplication | Some implicit assumptions | Fragile, high change cost |
| ECS compatibility | Data-oriented, system-friendly | Mixed OO/ECS | Fights ECS model |
| Editor/runtime separation | Runtime has zero editor deps | Minor leaks | Runtime requires editor |
| Public API | Intuitive, hard to misuse | Inconsistent edges | Confusing, leaky |
| Extensibility | New features slot in cleanly | Requires careful work | Blocked by core design |

## Review principles

- Architectural correctness over local code quality; root causes over symptoms.
- Recommend fundamental redesign when it clearly beats incremental fixes.
- Challenge existing design and your own recommendations with equal rigor.
- Compare with mature engines (Unity, Godot, etc.) — explain trade-offs, don't assume one winner.
- Evidence-driven: explain *why* problems exist and *why* they will matter later.
